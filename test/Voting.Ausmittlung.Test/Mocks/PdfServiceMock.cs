// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Report.Services;
using Voting.Lib.DmDoc.Serialization.Xml;

namespace Voting.Ausmittlung.Test.Mocks;

/// <summary>
/// Pdf service mock which returns the DmDoc XML for testing purposes.
/// </summary>
public class PdfServiceMock : IPdfService
{
    private readonly Dictionary<string, string> _generatedByTemplateName = new();

    public Task<Stream> Render<T>(string templateName, T data)
    {
        var bytes = Encoding.UTF8.GetBytes(DmDocXmlSerializer.Serialize(data));
        Stream stream = new MemoryStream(bytes);
        return Task.FromResult(stream);
    }

    public Task StartPdfGeneration<T>(string templateName, T data, string webhookUrl)
    {
        _generatedByTemplateName[templateName] = DmDocXmlSerializer.Serialize(data);
        return Task.CompletedTask;
    }

    public Task<Stream> GetPdf(int printJobId, CancellationToken ct = default)
        => throw new InvalidOperationException("Not supported");

    // Returns the DmDoc XML for StartPdfGeneration calls only.
    public string GetGenerated(string templateName)
        => _generatedByTemplateName[templateName];
}
