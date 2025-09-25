// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class AggregatedContestCountingCircleDetailsUtil
{
    public static void AdjustAggregatedDetails<TAggregatedDetails, TCountOfVotersInformationSubTotal, TVotingCardResultDetail>(
        TAggregatedDetails aggregatedDetails,
        ContestCountingCircleDetails countingCircleDetails,
        int deltaFactor)
        where TAggregatedDetails : AggregatedContestCountingCircleDetails<TCountOfVotersInformationSubTotal, TVotingCardResultDetail>
        where TCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal, new()
        where TVotingCardResultDetail : AggregatedVotingCardResultDetail, new()
    {
        AdjustCountOfVotersInfo(
            aggregatedDetails.CountOfVotersInformationSubTotals,
            countingCircleDetails.CountOfVotersInformationSubTotals,
            deltaFactor);
        AdjustContestDetailsVotingCards(
            aggregatedDetails.VotingCards,
            countingCircleDetails.VotingCards,
            deltaFactor);
    }

    public static void AdjustContestDetailsVotingCards<TVotingCardResultDetail>(
        ICollection<TVotingCardResultDetail> aggregatedVotingCards,
        ICollection<VotingCardResultDetail> votingCards,
        int deltaFactor)
        where TVotingCardResultDetail : AggregatedVotingCardResultDetail, new()
    {
        var votingCardsByUniqueKey = aggregatedVotingCards.ToDictionary(x => (x.Channel, x.Valid, x.DomainOfInfluenceType));
        foreach (var votingCard in votingCards)
        {
            votingCardsByUniqueKey.TryGetValue((votingCard.Channel, votingCard.Valid, votingCard.DomainOfInfluenceType), out var matchingVotingCard);
            if (matchingVotingCard == null)
            {
                matchingVotingCard = new()
                {
                    Channel = votingCard.Channel,
                    Valid = votingCard.Valid,
                    DomainOfInfluenceType = votingCard.DomainOfInfluenceType,
                };

                aggregatedVotingCards.Add(matchingVotingCard);
            }

            matchingVotingCard.CountOfReceivedVotingCards += votingCard.CountOfReceivedVotingCards.GetValueOrDefault() * deltaFactor;
        }
    }

    private static void AdjustCountOfVotersInfo<TAggregatedCountOfVotersInformationSubTotal>(
        ICollection<TAggregatedCountOfVotersInformationSubTotal> aggregatedCountOfVotersInformationSubTotals,
        ICollection<CountOfVotersInformationSubTotal> countOfVotersInformationSubTotals,
        int deltaFactor)
        where TAggregatedCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal, new()
    {
        var subTotalsBySexAndVoterTypeAndDoiType = aggregatedCountOfVotersInformationSubTotals.ToDictionary(x => (x.Sex, x.VoterType, x.DomainOfInfluenceType));
        foreach (var countOfVotersInfoSubTotal in countOfVotersInformationSubTotals)
        {
            subTotalsBySexAndVoterTypeAndDoiType.TryGetValue((countOfVotersInfoSubTotal.Sex, countOfVotersInfoSubTotal.VoterType, countOfVotersInfoSubTotal.DomainOfInfluenceType), out var matchingSubTotal);
            if (matchingSubTotal == null)
            {
                matchingSubTotal = new()
                {
                    VoterType = countOfVotersInfoSubTotal.VoterType,
                    Sex = countOfVotersInfoSubTotal.Sex,
                    DomainOfInfluenceType = countOfVotersInfoSubTotal.DomainOfInfluenceType,
                };

                aggregatedCountOfVotersInformationSubTotals.Add(matchingSubTotal);
            }

            matchingSubTotal.CountOfVoters += countOfVotersInfoSubTotal.CountOfVoters.GetValueOrDefault() * deltaFactor;
        }
    }
}
