// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class MajorityElectionCandidateDomainOfInfluenceResult
{
    public MajorityElectionCandidateDomainOfInfluenceResult(MajorityElectionCandidateBase candidate)
    {
        Candidate = candidate;
    }

    public MajorityElectionCandidateBase Candidate { get; set; }

    public int VoteCount { get; set; }
}
