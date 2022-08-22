// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class AggregatedContestCountingCircleDetails<TCountOfVotersInformationSubTotal, TVotingCardResultDetail> : BaseEntity
    where TCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal, new()
    where TVotingCardResultDetail : AggregatedVotingCardResultDetail, new()
{
    public ICollection<TCountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; set; } = new HashSet<TCountOfVotersInformationSubTotal>();

    public ICollection<TVotingCardResultDetail> VotingCards { get; set; } = new HashSet<TVotingCardResultDetail>();

    public int TotalCountOfVoters { get; set; }

    public void OrderVotingCardsAndSubTotals()
    {
        VotingCards = VotingCards
            .OrderBy(x => x.DomainOfInfluenceType)
            .OrderByPriority() // order by priority is stable in sorting, therefore respects the existing by doi type sort.
            .ToList();

        CountOfVotersInformationSubTotals = CountOfVotersInformationSubTotals
            .OrderBy(x => x.Sex)
            .ThenBy(x => x.VoterType)
            .ThenBy(x => x.CountOfVoters)
            .ToList();
    }
}
