// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices;

namespace Voting.Ausmittlung.Core.Services.Export;

public interface IExportProviderUploader
{
    ExportProvider Provider { get; }

    Task RenderAndUpload(
        ResultExportConfiguration export,
        IEnumerable<ReportRenderContext> reportContexts,
        CancellationToken ct);
}
