// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfCountingCircleResult
{
    public PdfCountingCircle? CountingCircle { get; set; }

    public int TotalCountOfVoters { get; set; }

    public CountingCircleResultState State { get; set; }

    public bool IsAuditedTentativelyOrPlausibilised { get; set; }
}
