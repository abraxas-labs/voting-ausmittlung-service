// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Voting.Lib.DmDoc;

namespace Voting.Ausmittlung.Report.Services;

public class DmDocPdfService : IPdfService
{
    private readonly IDmDocService _dmDoc;

    public DmDocPdfService(IDmDocService dmDoc)
    {
        _dmDoc = dmDoc;
    }

    public Task<Stream> Render<T>(string templateName, T data)
        => _dmDoc.FinishAsPdf(templateName, data);

    public Task StartPdfGeneration<T>(string templateName, T data, string webhookUrl)
        => _dmDoc.StartAsyncPdfGeneration(templateName, data, webhookUrl);

    public Task<Stream> GetPdf(int printJobId, CancellationToken ct = default)
        => _dmDoc.GetPdfForPrintJob(printJobId, ct);
}
