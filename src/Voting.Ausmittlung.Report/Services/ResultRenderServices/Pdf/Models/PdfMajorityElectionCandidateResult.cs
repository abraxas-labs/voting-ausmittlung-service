// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionCandidateResult
{
    public PdfMajorityElectionCandidate? Candidate { get; set; }

    public int VoteCount { get; set; }
}
