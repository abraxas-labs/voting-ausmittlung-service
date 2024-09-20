// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.Common;
using Voting.Lib.DokConnector.Service;

namespace Voting.Ausmittlung.Core.Services.Export;

public class SeantisProviderUploader : IExportProviderUploader
{
    private const int AesKeySize = 256;
    private const string TokenFileName = "Token.txt";
    private const string ZipFileExtension = ".zip";
    private readonly IDokConnector _dokConnector;
    private readonly IClock _clock;
    private readonly ILogger<SeantisProviderUploader> _logger;
    private readonly ExportService _exportService;
    private readonly SeantisConfig _config;

    public SeantisProviderUploader(
        IDokConnector dokConnector,
        AppConfig appConfig,
        IClock clock,
        ILogger<SeantisProviderUploader> logger,
        ExportService exportService)
    {
        _dokConnector = dokConnector;
        _clock = clock;
        _logger = logger;
        _exportService = exportService;
        _config = appConfig.Publisher.Seantis;
    }

    public ExportProvider Provider => ExportProvider.Seantis;

    public async Task RenderAndUpload(
        ResultExportConfiguration export,
        IEnumerable<ReportRenderContext> reportContexts,
        CancellationToken ct)
    {
        // For Seantis, we need to group all the report contexts by the Seantis token
        // For each unique token, a ZIP file should be generated
        var reportContextsByToken = reportContexts
            .GroupBy(x => export.PoliticalBusinessMetadata!.FirstOrDefault(m =>
                m.PoliticalBusinessId == x.PoliticalBusinessIds.First())?.Token ?? string.Empty)
            .ToDictionary(x => x.Key, x => x.ToList());

        foreach (var (token, contexts) in reportContextsByToken.OrderBy(x => x.Key))
        {
            if (string.IsNullOrEmpty(token))
            {
                var pbId = contexts[0].PoliticalBusinessIds.First();
                throw new InvalidOperationException($"No Seantis token configured for political business {pbId}");
            }

            await UploadZip(contexts, token, export.EaiMessageType, ct);
        }
    }

    private async Task UploadZip(
        List<ReportRenderContext> reportContexts,
        string token,
        string eaiMessageType,
        CancellationToken ct)
    {
        var zipPipe = new Pipe();

        // Start the upload task with the not yet completed stream. This allows us to stream the ZIP without storing it in memory.
        var zipFileName = token + ZipFileExtension;
        var uploadTask = _dokConnector.Upload(eaiMessageType, zipFileName, zipPipe.Reader.AsStream(), ct);

        // The ZIP output streams writes into the pipe, which pipes directly into the DOK connector uploader
        await using var outputStream = new ZipOutputStream(zipPipe.Writer.AsStream());
        outputStream.Password = _config.ZipPassword;

        // Write the token file
        var tokenEntry = new ZipEntry(TokenFileName)
        {
            AESKeySize = AesKeySize,
            CompressionMethod = CompressionMethod.Deflated,
            DateTime = _clock.UtcNow,
        };
        await outputStream.PutNextEntryAsync(tokenEntry, ct);
        outputStream.Write(Encoding.UTF8.GetBytes(token));

        // Render each report context and immediately stream it to the upload task
        foreach (var reportContext in reportContexts)
        {
            FileModel file;
            try
            {
                file = await _exportService.GenerateResultExport(reportContext, ct);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error rendering report {@ReportContext}", reportContext);
                continue;
            }

            var fileEntry = new ZipEntry(file.Filename)
            {
                AESKeySize = AesKeySize,
                CompressionMethod = CompressionMethod.Deflated,
                DateTime = _clock.UtcNow,
            };
            await outputStream.PutNextEntryAsync(fileEntry, ct);

            // Write the file
            var filePipeWriter = PipeWriter.Create(outputStream, new StreamPipeWriterOptions(leaveOpen: true));
            await file.Write(filePipeWriter, ct);
            await filePipeWriter.CompleteAsync();
        }

        // Finish writing the ZIP file
        await outputStream.FinishAsync(ct);
        await zipPipe.Writer.CompleteAsync();

        await uploadTask;
    }
}
