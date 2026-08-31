// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        var updateVerifiedPdfs = args.Contains("--update", StringComparer.OrdinalIgnoreCase);

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
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));

        await host.StartAsync();
        await CreatePdfs(host.Services, logger);
        await host.StopAsync();

        if (updateVerifiedPdfs)
        {
            UpdateVerifiedPdfs(logger);
        }

        // pipeline job should fail if diffs exists
        if (Directory.GetFiles(DiffFolder).Length > 0)
        {
            Environment.Exit(1);
        }
    }

    private static async Task CreatePdfs(IServiceProvider hostProvider, ILogger logger)
    {
        using var serviceScope = hostProvider.CreateScope();
        var provider = serviceScope.ServiceProvider;
        var pdfService = provider.GetRequiredService<IPdfService>();

        var filePaths = Directory.GetFiles(XmlSnapshotFolder, "*.xml", SearchOption.TopDirectoryOnly).Order();
        foreach (var filePath in filePaths)
        {
            var filePathWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            if (filePath.Contains(ActivityProtocolKey))
            {
                var data = ReadXml<PdfActivityProtocol>(filePath);
                await GeneratePdf(pdfService, data, data.TemplateKey, filePathWithoutExtension, logger);
            }
            else if (filePath.Contains(BundleReviewKey))
            {
                var data = ReadXml<PdfPoliticalBusinessResultBundleReview>(filePath);
                await GeneratePdf(pdfService, data, data.TemplateKey, filePathWithoutExtension, logger);
            }
            else
            {
                var data = ReadXml<PdfTemplateBag>(filePath);
                await GeneratePdf(pdfService, data, data.TemplateKey, filePathWithoutExtension, logger, data.DomainOfInfluence?.Canton);
            }

            await DiffPdf(filePathWithoutExtension, logger);
        }
    }

    private static async Task GeneratePdf<T>(IPdfService pdfService, T data, string templateKey, string filePathWithoutExtension, ILogger logger, DomainOfInfluenceCanton? canton = null)
    {
        templateKey = ResolveTemplateKey(templateKey, filePathWithoutExtension, canton);

        logger.LogInformation("Generating PDF for '{FileName}' using template '{TemplateKey}'", filePathWithoutExtension, templateKey);

        var stream = await pdfService.Render(templateKey, data);

        var fileName = filePathWithoutExtension + ".received.pdf";
        await using var fileStream = File.Create(Path.Combine(PdfSnapshotFolder, fileName));
        await stream.CopyToAsync(fileStream);
    }

    // Determines the DmDoc template key to render with, mirroring the canton suffix convention used in
    // production (see ResultExportService.GenerateResultExport): a template key can be suffixed with
    // '_<canton>' to select a canton-specific template variant.
    private static string ResolveTemplateKey(string templateKey, string filePathWithoutExtension, DomainOfInfluenceCanton? canton)
    {
        // Protocols in DmDoc are the same for secondary majority election and majority election.
        if (templateKey.StartsWith(AusmittlungPdfSecondaryMajorityElectionTemplates.SecondaryMajorityElectionTemplateKeyPrefix))
        {
            templateKey = templateKey[AusmittlungPdfSecondaryMajorityElectionTemplates.SecondaryMajorityElectionTemplateKeyPrefix.Length..];
        }

        // Double proportional protocols only exist for canton ZH, so always test these with the ZH canton suffix.
        if (templateKey.Contains(DoubleProportionalKey))
        {
            return templateKey + CantonSuffix(DomainOfInfluenceCanton.Zh);
        }

        // Other canton-specific template variants are opted into per snapshot via a matching '_<canton>' file name suffix.
        return canton.HasValue && filePathWithoutExtension.EndsWith(CantonSuffix(canton.Value), StringComparison.Ordinal)
            ? templateKey + CantonSuffix(canton.Value)
            : templateKey;
    }

    private static string CantonSuffix(DomainOfInfluenceCanton canton)
        => $"_{canton.ToString().ToLower(CultureInfo.InvariantCulture)}";

    private static T ReadXml<T>(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var serializer = new XmlSerializer(typeof(T));
        return (T)serializer.Deserialize(stream)!;
    }

    // Accepts the newly rendered PDFs as the new baseline: for every remaining diff, the .received.pdf
    // becomes the new .verified.pdf and the now obsolete diff is removed. The verified PDF is written both
    // to the working directory's pdfs folder (bin/Debug/net8.0/pdfs) and to the project's pdfs folder
    // (test/Voting.Ausmittlung.PdfDiff/pdfs), so the update can be committed to source control.
    private static void UpdateVerifiedPdfs(ILogger logger)
    {
        var projectPdfSnapshotFolder = Path.Combine(GetProjectDirectory(), PdfSnapshotFolder);
        Directory.CreateDirectory(projectPdfSnapshotFolder);

        var diffFilePaths = Directory.GetFiles(DiffFolder, "*.diff.pdf", SearchOption.TopDirectoryOnly);
        foreach (var diffFilePath in diffFilePaths)
        {
            var fileName = Path.GetFileName(diffFilePath);
            var filePathWithoutExtension = fileName[..^".diff.pdf".Length];

            var receivedPdfPath = Path.Combine(PdfSnapshotFolder, filePathWithoutExtension + ".received.pdf");
            if (!File.Exists(receivedPdfPath))
            {
                continue;
            }

            var verifiedPdfFileName = filePathWithoutExtension + ".verified.pdf";
            File.Copy(receivedPdfPath, Path.Combine(PdfSnapshotFolder, verifiedPdfFileName), overwrite: true);
            File.Copy(receivedPdfPath, Path.Combine(projectPdfSnapshotFolder, verifiedPdfFileName), overwrite: true);
            File.Delete(diffFilePath);

            logger.LogInformation("Updated verified PDF for '{FileName}'", filePathWithoutExtension);
        }
    }

    private static async Task DiffPdf(string filePathWithoutExtension, ILogger logger)
    {
        var verifiedPdfPath = Path.Combine(PdfSnapshotFolder, filePathWithoutExtension + ".verified.pdf");
        var receivedPdfPath = Path.Combine(PdfSnapshotFolder, filePathWithoutExtension + ".received.pdf");
        var diffPdfPath = Path.Combine(DiffFolder, filePathWithoutExtension + ".diff.pdf");

        Directory.CreateDirectory(DiffFolder);

        var diffPdfArgs = new[]
        {
            "--verbose",
            "--skip-identical",
            "--mark-differences",
            $"--output-diff={diffPdfPath}",
            verifiedPdfPath,
            receivedPdfPath,
        };

        var result = await RunDiffPdf(diffPdfArgs);

        // If there are no changes in the pdfs, an output diff pdf is always generated with a blank page.
        // There is currently no way to tell if this empty pdf is being generated (the metadata of the pdf always changes)
        // other than using a regex on the output text.
        var match = RegexOutputDiffs().Match(result.StandardOutput);

        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Could not determine the diff-pdf result for '{filePathWithoutExtension}'. " +
                $"Standard output: '{result.StandardOutput}'. Standard error: '{result.StandardError}'.");
        }

        // Remove the file if there is no diff (0 pages differ).
        if (match.Groups[1].Value == "0")
        {
            File.Delete(diffPdfPath);
        }
        else
        {
            logger.LogInformation("Detected PDF diff for '{FileName}' ({DiffCount} of {TotalCount} pages differ)", filePathWithoutExtension, match.Groups[1].Value, match.Groups[2].Value);
        }
    }

    private static async Task<BufferedCommandResult> RunDiffPdf(IReadOnlyList<string> args)
    {
        try
        {
            return await Cli.Wrap("diff-pdf")
                .WithArguments(args)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();
        }
        catch (Win32Exception)
        {
            // diff-pdf is not installed/on the PATH locally (e.g. on Windows, which has no official diff-pdf
            // build). Fall back to running it inside the 'pdf-diff-tools' docker compose service
            // (see docker-compose.yml), which must be started beforehand via `docker compose up -d`.
            // Paths must use '/' since the tool runs inside a Linux container.
            var dockerArgs = new[] { "compose", "-f", GetDockerComposeFilePath(), "exec", "-T", "pdf-diff-tools", "diff-pdf" }
                .Concat(args.Select(a => a.Replace('\\', '/')));

            try
            {
                return await Cli.Wrap("docker")
                    .WithArguments(dockerArgs)
                    .WithValidation(CommandResultValidation.None)
                    .ExecuteBufferedAsync();
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException(
                    "Could not run 'diff-pdf', neither directly nor via docker. Either install diff-pdf and make " +
                    "it available on the PATH, or install docker and start the 'pdf-diff-tools' compose service " +
                    "(`docker compose up -d`, from the Voting.Ausmittlung.PdfDiff project directory). " +
                    "See README-internal.md ('PDF diff test' section) for details.",
                    ex);
            }
        }
    }

    [GeneratedRegex("(\\d+) of (\\d+) pages differ.")]
    private static partial Regex RegexOutputDiffs();

    // docker-compose.yml lives next to Program.cs. Using [CallerFilePath] (via GetProjectDirectory) instead of a
    // hardcoded relative path keeps this working regardless of the build output layout (TFM, configuration, etc.).
    private static string GetDockerComposeFilePath()
        => Path.Combine(GetProjectDirectory(), "docker-compose.yml");

    private static string GetProjectDirectory([CallerFilePath] string sourceFilePath = "")
        => Path.GetDirectoryName(sourceFilePath)!;
}
