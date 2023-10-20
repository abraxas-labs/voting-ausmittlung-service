// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfMajorityElectionCandidateResult
{
    public PdfMajorityElectionCandidate? Candidate { get; set; }

    public int ConventionalVoteCount { get; set; }

    public int EVotingExclWriteInsVoteCount { get; set; }

    public int EVotingWriteInsVoteCount { get; set; }

    public int EVotingInclWriteInsVoteCount { get; set; }

    public int VoteCount { get; set; }
}
