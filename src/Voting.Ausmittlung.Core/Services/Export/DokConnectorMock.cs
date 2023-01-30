// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voting.Lib.DokConnector.Models;
using Voting.Lib.DokConnector.Service;

namespace Voting.Ausmittlung.Core.Services.Export;

public class DokConnectorMock : IDokConnector
{
    private const string PdfFileExtension = ".pdf";
    private const string ZipFileExtension = ".zip";
    private readonly ILogger<DokConnectorMock> _logger;

    public DokConnectorMock(ILogger<DokConnectorMock> logger)
    {
        _logger = logger;
    }

    public async Task<UploadResponse> Upload(string messageType, string fileName, Stream fileContent, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await fileContent.CopyToAsync(ms, ct);
        return WriteToLog(messageType, fileName, ms.ToArray());
    }

    public async Task<UploadResponse> Upload(
        string messageType,
        string fileName,
        Func<PipeWriter, CancellationToken, Task> writer,
        CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        var pipeWriter = PipeWriter.Create(ms);
        await writer(pipeWriter, ct);
        await pipeWriter.FlushAsync(ct);
        return WriteToLog(messageType, fileName, ms.ToArray());
    }

    private UploadResponse WriteToLog(string messageType, string fileName, byte[] content)
    {
        object contentLogArg;
        if (fileName.EndsWith(PdfFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            // If it is a PDF, the content is an object containing data for templating, not the PDF itself
            contentLogArg = content;
        }
        else if (fileName.EndsWith(ZipFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            contentLogArg = Convert.ToBase64String(content);
        }
        else
        {
            contentLogArg = Encoding.UTF8.GetString(content);
        }

        _logger.LogInformation(
            "file saved: {MessageType} {FileName}\n{Content}",
            messageType,
            fileName,
            contentLogArg);

        var fileId = Guid.NewGuid().ToString();
        return new UploadResponse(fileId);
    }
}
