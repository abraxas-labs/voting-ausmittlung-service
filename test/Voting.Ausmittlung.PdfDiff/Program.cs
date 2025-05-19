// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.PdfDiff;

public static partial class Program
{
    private const string XmlSnapshotFolder = "xmls";
    private const string PdfSnapshotFolder = "pdfs";
    private const string DiffFolder = "diffs";
    private const string ActivityProtocolKey = "activity_protocol";
    private const string BundleReviewKey = "bundle_review";
    private const string DoubleProportionalKey = "double_proportional";

    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        var appConfig = configBuilder.Build().Get<AppConfig>();

        builder.Services.AddHttpClient();
        builder.Services.AddDmDoc(appConfig!.Publisher.Documatrix);
        builder.Services.AddSingleton<IPdfService, DmDocPdfService>();

        using var host = builder.Build();
        await host.StartAsync();
        await CreatePdfs(host.Services);
        await host.StopAsync();

        // pipeline job should fail if diffs exists
        if (Directory.GetFiles(DiffFolder).Length > 0)
        {
            Environment.Exit(1);
        }
    }

    private static async Task CreatePdfs(IServiceProvider hostProvider)
    {
        using var serviceScope = hostProvider.CreateScope();
        var provider = serviceScope.ServiceProvider;
        var pdfService = provider.GetRequiredService<IPdfService>();

        var filePaths = Directory.GetFiles(XmlSnapshotFolder, "*.xml", SearchOption.TopDirectoryOnly);
        foreach (var filePath in filePaths)
        {
            var filePathWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            if (filePath.Contains(ActivityProtocolKey))
            {
                var data = ReadXml<PdfActivityProtocol>(filePath);
                await GeneratePdf(pdfService, data, data.TemplateKey, filePathWithoutExtension);
            }
            else if (filePath.Contains(BundleReviewKey))
            {
                var data = ReadXml<PdfPoliticalBusinessResultBundleReview>(filePath);
                await GeneratePdf(pdfService, data, data.TemplateKey, filePathWithoutExtension);
            }
            else
            {
                var data = ReadXml<PdfTemplateBag>(filePath);
                await GeneratePdf(pdfService, data, data.TemplateKey, filePathWithoutExtension);
            }

            await DiffPdf(filePathWithoutExtension);
        }
    }

    private static async Task GeneratePdf<T>(IPdfService pdfService, T data, string templateKey, string filePathWithoutExtension)
    {
        // Protocols in DmDoc are the same for secondary majority election and majority election.
        if (templateKey.StartsWith(AusmittlungPdfSecondaryMajorityElectionTemplates.SecondaryMajorityElectionTemplateKeyPrefix))
        {
            templateKey = templateKey[AusmittlungPdfSecondaryMajorityElectionTemplates.SecondaryMajorityElectionTemplateKeyPrefix.Length..];
        }

        // double proportional protocols are only available for canton ZH, test these protocols with ZH canton suffix
        if (templateKey.Contains(DoubleProportionalKey))
        {
            templateKey += $"_{nameof(DomainOfInfluenceCanton.Zh).ToLower(CultureInfo.InvariantCulture)}";
        }

        var stream = await pdfService.Render(templateKey, data);

        var fileName = filePathWithoutExtension + ".received.pdf";
        await using var fileStream = File.Create(Path.Combine(PdfSnapshotFolder, fileName));
        await stream.CopyToAsync(fileStream);
    }

    private static T ReadXml<T>(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(stream)!;
    }

    private static async Task DiffPdf(string filePathWithoutExtension)
    {
        var verifiedPdfPath = Path.Combine(PdfSnapshotFolder, filePathWithoutExtension + ".verified.pdf");
        var receivedPdfPath = Path.Combine(PdfSnapshotFolder, filePathWithoutExtension + ".received.pdf");
        var diffPdfPath = Path.Combine(DiffFolder, filePathWithoutExtension + ".diff.pdf");

        Directory.CreateDirectory(DiffFolder);

        var result = await Cli.Wrap("diff-pdf")
            .WithArguments(["--verbose", "--skip-identical", "--mark-differences", $"--output-diff={diffPdfPath}", verifiedPdfPath, receivedPdfPath])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        // If there are no changes in the pdfs, an output diff pdf is always generated with a blank page.
        // There is currently no way to tell if this empty pdf is being generated (the metadata of the pdf always changes)
        // other than using a regex on the output text.
        var match = RegexOutputDiffs().Match(result.StandardOutput);

        // Remove the file if there is no diff (0 pages differ).
        if (match.Groups.Count == 2 && match.Groups[1].Value == "0")
        {
            File.Delete(diffPdfPath);
        }
    }

    [GeneratedRegex("(\\d+) of \\d+ pages differ.")]
    private static partial Regex RegexOutputDiffs();
}
