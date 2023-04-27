// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Models;
using Voting.Ausmittlung.Report.Services;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;
using Voting.Lib.DokConnector.Service;

namespace Voting.Ausmittlung.Core.Services.Export;

public class StandardProviderUploader : IExportProviderUploader
{
    private readonly IDokConnector _dokConnector;
    private readonly ILogger<StandardProviderUploader> _logger;
    private readonly ExportService _exportService;

    public StandardProviderUploader(IDokConnector dokConnector, ILogger<StandardProviderUploader> logger, ExportService exportService)
    {
        _dokConnector = dokConnector;
        _logger = logger;
        _exportService = exportService;
    }

    public ExportProvider Provider => ExportProvider.Standard;

    public async Task RenderAndUpload(
        ResultExportConfiguration export,
        IEnumerable<ReportRenderContext> reportContexts,
        Func<FileModel, string, Task> afterUpload,
        CancellationToken ct)
    {
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

            try
            {
                var response = await _dokConnector.Upload(export.EaiMessageType, file.Filename, file.Write, ct);
                await afterUpload(file, response.FileId);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "could not save export {FileName} {TemplateKey} {ExportId}",
                    file.Filename,
                    reportContext.Template.Key,
                    export.ExportConfigurationId);
            }
        }
    }
}
