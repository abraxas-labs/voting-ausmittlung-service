// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
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
        params string[] filenameArgs)
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);
        return new FileModel(context, fileName, ExportFileFormat.Csv, (w, ct) => _csvService.Render(w, records, ct));
    }

    public FileModel RenderToDynamicCsv<TRow>(
        ReportRenderContext context,
        IEnumerable<TRow> records,
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
        var content = await _pdfService.Render(context.Template.Key, data);
        return new FileModel(context, fileName, ExportFileFormat.Pdf, async (w, ct) => await w.WriteAsync(content, ct));
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
