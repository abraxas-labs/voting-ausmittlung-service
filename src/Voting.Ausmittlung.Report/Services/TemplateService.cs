// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsvHelper;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Services;

public class TemplateService
{
    private readonly CsvService _csvService;
    private readonly IPdfService _pdfService;

    public TemplateService(
        CsvService csvService,
        IPdfService pdfService)
    {
        _csvService = csvService;
        _pdfService = pdfService;
    }

    public FileModel RenderToCsv<TRow>(
        ReportRenderContext context,
        IAsyncEnumerable<TRow> records,
        params string[] filenameArgs)
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);
        return new FileModel(context, fileName, ExportFileFormat.Csv, (w, ct) => _csvService.Render(w, records, ct));
    }

    public FileModel RenderToCsv<TRow>(
        ReportRenderContext context,
        IEnumerable<TRow> records,
        Action<IWriter>? configure = null,
        params string[] filenameArgs)
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);
        return new FileModel(context, fileName, ExportFileFormat.Csv, (w, ct) => _csvService.Render(w, records, configure, ct));
    }

    public FileModel RenderToDynamicCsv<TRow>(
        ReportRenderContext context,
        IAsyncEnumerable<TRow> records,
        params string[] filenameArgs)
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);
        return new FileModel(context, fileName, ExportFileFormat.Csv, (w, ct) => _csvService.RenderDynamic(w, records, ct));
    }

    public async Task<FileModel> RenderToPdf<T>(
        ReportRenderContext context,
        T data,
        params string[] filenameArgs)
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);

        if (context.AsyncPdfGenerationInfo is { } asyncPdfInfo)
        {
            await _pdfService.StartPdfGeneration(context.Template.Key, data, asyncPdfInfo.WebhookUrl);
            return new FileModel(
                context,
                fileName,
                ExportFileFormat.Pdf,
                (_, _) => throw new InvalidOperationException("This is an asynchronous export and does not have any content."));
        }

        var contentStream = await _pdfService.Render(context.Template.Key, data);
        return new FileModel(context, fileName, ExportFileFormat.Pdf, async (w, ct) =>
        {
            try
            {
                await contentStream.CopyToAsync(w.AsStream(), ct);
            }
            finally
            {
                await contentStream.DisposeAsync();
            }
        });
    }

    public FileModel RenderToXml(
        ReportRenderContext context,
        string messageId,
        object data,
        params string[] filenameArgs)
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);
        return new FileModel(context, fileName, ExportFileFormat.Xml, messageId, (w, _) =>
        {
            EchSerializer.ToXml(w, data);
            return Task.CompletedTask;
        });
    }
}
