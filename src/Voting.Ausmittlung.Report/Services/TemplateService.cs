// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CsvHelper;
using Ech0222_1_0;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.Ech;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Report.Services;

public class TemplateService
{
    // Needs to match the order of ElectionRawDataTypeBallotRawDataBallotPosition.IsEmpty
    private const int PositionIsEmptyXmlAttributeOrder = 1;
    private const string PositionIsEmptyXmlAttributeName = "isEmpty";

    private readonly CsvService _csvService;
    private readonly IPdfService _pdfService;
    private readonly EchSerializer _echSerializer;

    public TemplateService(
        CsvService csvService,
        IPdfService pdfService,
        EchSerializer echSerializer)
    {
        _csvService = csvService;
        _pdfService = pdfService;
        _echSerializer = echSerializer;
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

    public FileModel RenderToXml<T>(
        ReportRenderContext context,
        string messageId,
        T data,
        params string[] filenameArgs)
        where T : notnull
    {
        var fileName = FileNameBuilder.GenerateFileName(context.Template, filenameArgs);
        return new FileModel(context, fileName, ExportFileFormat.Xml, messageId, (w, _) =>
        {
            _echSerializer.WriteXml(w, data, BuildXmlAttributeOverrides());
            return Task.CompletedTask;
        });
    }

    private XmlAttributeOverrides BuildXmlAttributeOverrides()
    {
        var xmlAttributeOverrides = new XmlAttributeOverrides();
        var attributes = new XmlAttributes();

        var elementAttribute = new XmlElementAttribute(PositionIsEmptyXmlAttributeName) { Order = PositionIsEmptyXmlAttributeOrder };
        attributes.XmlElements.Add(elementAttribute);

        // ensure that isEmpty element is serialized
        attributes.XmlDefaultValue = false;

        xmlAttributeOverrides.Add(typeof(ElectionRawDataTypeBallotRawDataBallotPosition), nameof(ElectionRawDataTypeBallotRawDataBallotPosition.IsEmpty), attributes);
        return xmlAttributeOverrides;
    }
}
