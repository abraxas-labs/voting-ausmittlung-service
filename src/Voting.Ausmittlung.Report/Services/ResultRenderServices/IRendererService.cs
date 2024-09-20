// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Report.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices;

public interface IRendererService
{
    Task<FileModel> Render(ReportRenderContext ctx, CancellationToken ct = default);
}
