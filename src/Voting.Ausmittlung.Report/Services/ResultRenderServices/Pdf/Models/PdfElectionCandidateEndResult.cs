// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public abstract class PdfElectionCandidateEndResult
{
    public int Rank { get; set; }

    public int VoteCount { get; set; }

    public bool LotDecision { get; set; }

    public bool LotDecisionRequired { get; set; }
}
