// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ContestCountingCircleDetails : BaseEntity
{
    public Guid ContestId { get; set; }

    public Contest Contest { get; set; } = null!;

    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!;

    public ICollection<CountOfVotersInformationSubTotal> CountOfVotersInformationSubTotals { get; set; } = new HashSet<CountOfVotersInformationSubTotal>();

    public ICollection<VotingCardResultDetail> VotingCards { get; set; } = new HashSet<VotingCardResultDetail>();

    public bool EVoting { get; set; }

    public bool ECounting { get; set; }

    public bool ECountingResultsImported { get; set; }

    public CountingMachine CountingMachine { get; set; }

    public (int Valid, int Invalid) SumVotingCards(DomainOfInfluenceType doiType)
    {
        return SumValidInvalidVotingCards(VotingCards.Where(x => x.DomainOfInfluenceType == doiType));
    }

    public int SumTotalCountOfVoters(DomainOfInfluenceType doiType)
    {
        return CountOfVotersInformationSubTotals
            .Where(st => st.DomainOfInfluenceType == doiType)
            .Sum(st => st.CountOfVoters.GetValueOrDefault());
    }

    public void OrderVotingCardsAndSubTotals()
    {
        VotingCards = VotingCards
            .OrderBy(x => x.DomainOfInfluenceType)
            .ThenBy(x => x.Valid)
            .ThenBy(x => x.Channel)
            .ThenBy(x => x.CountOfReceivedVotingCards)
            .ToList();

        CountOfVotersInformationSubTotals = CountOfVotersInformationSubTotals
            .OrderBy(x => x.DomainOfInfluenceType)
            .ThenBy(x => x.Sex)
            .ThenBy(x => x.VoterType)
            .ThenBy(x => x.CountOfVoters)
            .ToList();
    }

    public void ResetVotingCardsAndSubTotals()
    {
        foreach (var votingCard in VotingCards)
        {
            votingCard.CountOfReceivedVotingCards = null;
        }

        foreach (var countOfVoters in CountOfVotersInformationSubTotals)
        {
            countOfVoters.CountOfVoters = null;
        }
    }

    private (int Valid, int Invalid) SumValidInvalidVotingCards(IEnumerable<VotingCardResultDetail> details)
    {
        var values = details
            .GroupBy(x => x.Valid)
            .ToDictionary(x => x.Key, x => x.Sum(y => y.CountOfReceivedVotingCards ?? 0));
        values.TryGetValue(true, out var valid);
        values.TryGetValue(false, out var invalid);
        return (valid, invalid);
    }
}
