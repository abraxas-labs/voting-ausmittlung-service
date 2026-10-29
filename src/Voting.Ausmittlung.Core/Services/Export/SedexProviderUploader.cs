// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.Common;
using Voting.Lib.DokConnector.Service;
using Voting.Lib.Ech.Ech0090_1_0.Schemas;

namespace Voting.Ausmittlung.Core.Services.Export;

public class SedexProviderUploader : IExportProviderUploader
{
    private const string ZipFileExtension = ".zip";
    private const string EnvelopeFileName = "envelope.xml";

    private readonly IDokConnector _dokConnector;
    private readonly EchValidationSerializer _echSerializer;
    private readonly Ech0090Serializer _ech0090Serializer;
    private readonly IClock _clock;
    private readonly ILogger<SedexProviderUploader> _logger;
    private readonly ExportService _exportService;
    private readonly SedexConfig _sedexConfig;

    public SedexProviderUploader(
        IDokConnector dokConnector,
        AppConfig appConfig,
        EchValidationSerializer echSerializer,
        Ech0090Serializer ech0090Serializer,
        IClock clock,
        ILogger<SedexProviderUploader> logger,
        ExportService exportService)
    {
        _dokConnector = dokConnector;
        _echSerializer = echSerializer;
        _ech0090Serializer = ech0090Serializer;
        _clock = clock;
        _logger = logger;
        _sedexConfig = appConfig.Publisher.Sedex;
        _exportService = exportService;
    }

    public ExportProvider Provider => ExportProvider.Sedex;

    public async Task RenderAndUpload(
        ResultExportConfiguration export,
        IEnumerable<ReportRenderContext> reportContexts,
        CancellationToken ct)
    {
        foreach (var reportContext in reportContexts)
        {
            await UploadSedexZip(reportContext, export.ExportConfiguration!, ct);
        }
    }

    private async Task UploadSedexZip(
        ReportRenderContext reportContext,
        ExportConfiguration exportConfiguration,
        CancellationToken ct)
    {
        FileModel file;
        try
        {
            file = await _exportService.GenerateResultExport(reportContext, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error rendering report {@ReportContext}", reportContext);
            return;
        }

        try
        {
            _logger.LogDebug(
                "Uploading {FileName} via Sedex with {EaiMessageType}",
                file.Filename,
                exportConfiguration.EaiMessageType);

            var zipPipe = new Pipe();

            // Start the upload task with the not yet completed stream. This allows us to stream the ZIP without storing it in memory.
            var zipFileName = Path.ChangeExtension(file.Filename, ZipFileExtension);
            var uploadTask = _dokConnector.Upload(exportConfiguration.EaiMessageType, zipFileName, zipPipe.Reader.AsStream(), ct);

            // The ZIP output streams writes into the pipe, which pipes directly into the DOK connector uploader
            await using var outputStream = new ZipOutputStream(zipPipe.Writer.AsStream());

            var envelopeEntry = new ZipEntry(EnvelopeFileName)
            {
                CompressionMethod = CompressionMethod.Deflated,
                DateTime = _clock.UtcNow,
            };
            await outputStream.PutNextEntryAsync(envelopeEntry, ct);
            var envelope = _ech0090Serializer.ToSedexEnvelope(_sedexConfig.MessageType, _sedexConfig.MessageClass, exportConfiguration.SedexSenderId!, exportConfiguration.SedexRecipientIds!.ToList());
            await _echSerializer.SerializeEchToXml(outputStream, envelope, Ech0090Schemas.LoadEch0090Schemas(), ct: ct);

            var fileEntry = new ZipEntry(file.Filename)
            {
                CompressionMethod = CompressionMethod.Deflated,
                DateTime = _clock.UtcNow,
            };
            await outputStream.PutNextEntryAsync(fileEntry, ct);

            // Write the file
            var filePipeWriter = PipeWriter.Create(outputStream, new StreamPipeWriterOptions(leaveOpen: true));
            await file.Write(filePipeWriter, ct);
            await filePipeWriter.CompleteAsync();

            // Finish writing the ZIP file
            await outputStream.FinishAsync(ct);
            await zipPipe.Writer.CompleteAsync();

            await uploadTask;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Could not save export {FileName} {TemplateKey} to Sedex",
                file.Filename,
                reportContext.Template.Key);
        }
    }
}
