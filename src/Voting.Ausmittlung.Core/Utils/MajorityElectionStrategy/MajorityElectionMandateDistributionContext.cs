// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;

public class MajorityElectionMandateDistributionContext<TMajorityElectionCandidateEndResultBase>
    where TMajorityElectionCandidateEndResultBase : MajorityElectionCandidateEndResultBase
{
    public MajorityElectionMandateDistributionContext(
        int numberOfMandates,
        IEnumerable<TMajorityElectionCandidateEndResultBase> candidateEndResults,
        int? absoluteMajority = null)
    {
        NumberOfMandates = numberOfMandates;
        AbsoluteMajority = absoluteMajority;

        SortedCandidateEndResultsByVoteCount = candidateEndResults
            .GroupBy(x => x.VoteCount)
            .ToDictionary(x => x.Key, x => x.OrderBy(x => x.Rank).ToList());
    }

    public int NumberOfMandates { get; }

    public IReadOnlyDictionary<int, List<TMajorityElectionCandidateEndResultBase>> SortedCandidateEndResultsByVoteCount { get; }

    public int? VoteCountWithRestMandatesInUncompletedLotDecision { get; set; }

    public int DistributedNumberOfMandates { get; private set; }

    public int? AbsoluteMajority { get; }

    public int RestMandates => NumberOfMandates - DistributedNumberOfMandates;

    public bool AllNumberOfMandatesDistributed => RestMandates == 0;

    public void IncreaseDistributedNumberOfMandates()
    {
        DistributedNumberOfMandates++;
    }
}
