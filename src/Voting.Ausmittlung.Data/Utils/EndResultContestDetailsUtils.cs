// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class EndResultContestDetailsUtils
{
    public static void AdjustEndResultContestDetails<TEndResult, TCountOfVoterInfo, TVotingCard>(
        TEndResult trackedEndResult,
        ContestCountingCircleDetails countingCircleDetails,
        DomainOfInfluence domainOfInfluence,
        int deltaFactor)
        where TEndResult : PoliticalBusinessEndResultBase, IEndResultDetail<TCountOfVoterInfo, TVotingCard>
        where TCountOfVoterInfo : EndResultCountOfVotersInformationSubTotal, new()
        where TVotingCard : EndResultVotingCardDetail, new()
    {
        trackedEndResult.TotalCountOfVoters += countingCircleDetails.GetTotalCountOfVotersForDomainOfInfluence(domainOfInfluence) * deltaFactor;

        foreach (var votingCard in countingCircleDetails.VotingCards)
        {
            var matchingVotingCard = trackedEndResult.VotingCards
                .FirstOrDefault(x => x.Valid == votingCard.Valid
                    && x.Channel == votingCard.Channel
                    && x.DomainOfInfluenceType == votingCard.DomainOfInfluenceType);
            if (matchingVotingCard != null)
            {
                matchingVotingCard.CountOfReceivedVotingCards += votingCard.CountOfReceivedVotingCards.GetValueOrDefault() * deltaFactor;
            }
            else if (deltaFactor > 0)
            {
                trackedEndResult.VotingCards.Add(new TVotingCard
                {
                    Channel = votingCard.Channel,
                    DomainOfInfluenceType = votingCard.DomainOfInfluenceType,
                    Valid = votingCard.Valid,
                    CountOfReceivedVotingCards = votingCard.CountOfReceivedVotingCards.GetValueOrDefault(),
                });
            }
        }

        foreach (var countOfVoterInfo in countingCircleDetails.CountOfVotersInformationSubTotals)
        {
            var matchingInfo = trackedEndResult.CountOfVotersInformationSubTotals
                .FirstOrDefault(x => x.Sex == countOfVoterInfo.Sex
                    && x.VoterType == countOfVoterInfo.VoterType
                    && x.DomainOfInfluenceType == countOfVoterInfo.DomainOfInfluenceType);
            if (matchingInfo != null)
            {
                matchingInfo.CountOfVoters += countOfVoterInfo.CountOfVoters.GetValueOrDefault() * deltaFactor;
            }
            else if (deltaFactor > 0 && HasVoterTypeSupport(domainOfInfluence, countOfVoterInfo.VoterType))
            {
                trackedEndResult.CountOfVotersInformationSubTotals.Add(new TCountOfVoterInfo
                {
                    Sex = countOfVoterInfo.Sex,
                    VoterType = countOfVoterInfo.VoterType,
                    CountOfVoters = countOfVoterInfo.CountOfVoters.GetValueOrDefault(),
                    DomainOfInfluenceType = countOfVoterInfo.DomainOfInfluenceType,
                });
            }
        }
    }

    private static bool HasVoterTypeSupport(DomainOfInfluence domainOfInfluence, VoterType voterType)
    {
        if (domainOfInfluence.SwissAbroadVotingRight != SwissAbroadVotingRight.OnEveryCountingCircle && voterType == VoterType.SwissAbroad)
        {
            return false;
        }

        if (!domainOfInfluence.HasForeignerVoters && voterType == VoterType.Foreigner)
        {
            return false;
        }

        if (!domainOfInfluence.HasMinorVoters && voterType == VoterType.Minor)
        {
            return false;
        }

        return true;
    }
}
