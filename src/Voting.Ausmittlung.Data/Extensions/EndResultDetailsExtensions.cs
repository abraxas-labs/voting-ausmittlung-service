// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public static class EndResultDetailsExtensions
{
    public static void OrderVotingCardsAndSubTotals<TCountOfVotersInfo, TVotingCard>(
        this IEndResultDetail<TCountOfVotersInfo, TVotingCard> endResultDetail)
        where TCountOfVotersInfo : EndResultCountOfVotersInformationSubTotal
        where TVotingCard : EndResultVotingCardDetail
    {
        endResultDetail.VotingCards = endResultDetail.VotingCards
            .OrderBy(x => x.DomainOfInfluenceType)
            .OrderByPriority()
            .ToList();

        endResultDetail.CountOfVotersInformationSubTotals = endResultDetail.CountOfVotersInformationSubTotals
            .OrderBy(x => x.Sex)
            .ThenBy(x => x.VoterType)
            .ThenBy(x => x.CountOfVoters)
            .ToList();
    }
}
