// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Report.Models;
using Voting.Lib.VotingExports.Models;

namespace Voting.Ausmittlung.Core.Services.Export;

public class DokConnectorMock : IDokConnector
{
    private readonly ILogger<DokConnectorMock> _logger;

    public DokConnectorMock(ILogger<DokConnectorMock> logger)
    {
        _logger = logger;
    }

    public async Task<string> Save(string eaiMessageType, FileModel file, CancellationToken ct)
    {
        // TODO replace with real implementation, as soon as we get some info about the interface
        var content = await file.ContentAsByteArray(ct);
        var contentLogArg = file.RenderContext.Template.Format == ExportFileFormat.Pdf
            ? (object)content
            : Encoding.UTF8.GetString(content);
        _logger.LogInformation(
            "file saved: {EaiMessageType} {Key} {FileName}\n{Content}",
            eaiMessageType,
            file.RenderContext.Template.Key,
            file.Filename,
            contentLogArg);
        return Guid.NewGuid().ToString();
    }
}
