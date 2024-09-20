// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class EndResultContestDetailsUtils
{
    public static void AdjustEndResultContestDetails<TEndResult, TCountOfVoterInfo, TVotingCard>(
        TEndResult trackedEndResult,
        ContestCountingCircleDetails countingCircleDetails,
        int deltaFactor)
        where TEndResult : PoliticalBusinessEndResultBase, IEndResultDetail<TCountOfVoterInfo, TVotingCard>
        where TCountOfVoterInfo : EndResultCountOfVotersInformationSubTotal, new()
        where TVotingCard : EndResultVotingCardDetail, new()
    {
        trackedEndResult.TotalCountOfVoters += countingCircleDetails.TotalCountOfVoters * deltaFactor;

        foreach (var votingCard in countingCircleDetails.VotingCards)
        {
            var matchingVotingCard = trackedEndResult.VotingCards
                .FirstOrDefault(x => x.Valid == votingCard.Valid
                    && x.Channel == votingCard.Channel
                    && x.DomainOfInfluenceType == votingCard.DomainOfInfluenceType);
            if (matchingVotingCard != null)
            {
                matchingVotingCard.CountOfReceivedVotingCards += votingCard.CountOfReceivedVotingCards * deltaFactor;
            }
            else if (deltaFactor > 0)
            {
                trackedEndResult.VotingCards.Add(new TVotingCard
                {
                    Channel = votingCard.Channel,
                    DomainOfInfluenceType = votingCard.DomainOfInfluenceType,
                    Valid = votingCard.Valid,
                    CountOfReceivedVotingCards = votingCard.CountOfReceivedVotingCards,
                });
            }
        }

        foreach (var countOfVoterInfo in countingCircleDetails.CountOfVotersInformationSubTotals)
        {
            var matchingInfo = trackedEndResult.CountOfVotersInformationSubTotals
                .FirstOrDefault(x => x.Sex == countOfVoterInfo.Sex
                    && x.VoterType == countOfVoterInfo.VoterType);
            if (matchingInfo != null)
            {
                matchingInfo.CountOfVoters += countOfVoterInfo.CountOfVoters * deltaFactor;
            }
            else if (deltaFactor > 0)
            {
                trackedEndResult.CountOfVotersInformationSubTotals.Add(new TCountOfVoterInfo
                {
                    Sex = countOfVoterInfo.Sex,
                    VoterType = countOfVoterInfo.VoterType,
                    CountOfVoters = countOfVoterInfo.CountOfVoters,
                });
            }
        }
    }
}
