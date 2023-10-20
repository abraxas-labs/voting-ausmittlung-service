// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionCandidateEndResult : PdfElectionCandidateEndResult
{
    public PdfMajorityElectionCandidate? Candidate { get; set; }

    public MajorityElectionCandidateEndResultState State { get; set; }

    public int ConventionalVoteCount { get; set; }

    public int EVotingVoteCount { get; set; }
}
