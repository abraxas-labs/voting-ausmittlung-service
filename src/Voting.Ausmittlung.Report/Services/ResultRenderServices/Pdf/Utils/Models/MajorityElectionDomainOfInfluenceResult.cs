// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class MajorityElectionDomainOfInfluenceResult : DomainOfInfluenceResult
{
    public int IndividualVoteCount { get; set; }

    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    public IEnumerable<MajorityElectionCandidateDomainOfInfluenceResult> CandidateResults => CandidateResultsByCandidateId.Values
        .OrderBy(x => x.Candidate.Position);

    public List<MajorityElectionResult> Results { get; set; } = new List<MajorityElectionResult>();

    internal Dictionary<Guid, MajorityElectionCandidateDomainOfInfluenceResult> CandidateResultsByCandidateId { get; } =
        new Dictionary<Guid, MajorityElectionCandidateDomainOfInfluenceResult>();

    public override void OrderCountingCircleResults(ContestCantonDefaults cantonDefaults)
    {
        Results = Results
            .OrderByCountingCircle(x => x.CountingCircle, cantonDefaults)
            .ToList();
    }
}
