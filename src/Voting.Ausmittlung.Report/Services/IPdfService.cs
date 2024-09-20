// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Voting.Ausmittlung.Report.Services;

public interface IPdfService
{
    Task<Stream> Render<T>(string templateName, T data);

    Task StartPdfGeneration<T>(string templateName, T data, string webhookUrl);

    Task<Stream> GetPdf(int printJobId, CancellationToken ct = default);
}
