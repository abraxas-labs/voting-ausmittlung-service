// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoVotingCardMapping
{
    internal static VotingCardInformationType ToVoteInfoVotingCardInfo(this CountingCircle countingCircle, DomainOfInfluenceType doiType)
    {
        var votingCards = countingCircle.ContestDetails.SingleOrDefault()?.VotingCards;
        return new VotingCardInformationType
        {
            CountOfVotingCardsReceivedPrematurelyInBallotBox = GetVotingCardValue(votingCards, VotingChannel.Paper, true, doiType),
            CountOfVotingCardsReceivedByEvoting = GetVotingCardValue(votingCards, VotingChannel.EVoting, true, doiType),
            CountOfVotingCardsReceivedByMail = GetVotingCardValue(votingCards, VotingChannel.ByMail, true, doiType),
            CountOfInvalidVotingCardsReceivedByMail = GetVotingCardValue(votingCards, VotingChannel.ByMail, false, doiType),
            CountOfVotingCardsReceivedInBallotBox = GetVotingCardValue(votingCards, VotingChannel.BallotBox, true, doiType),
        };
    }

    private static uint? GetVotingCardValue(
        ICollection<VotingCardResultDetail>? votingCards,
        VotingChannel channel,
        bool valid,
        DomainOfInfluenceType doiType)
    {
        if (votingCards == null)
        {
            return null;
        }

        return (uint?)votingCards
            .SingleOrDefault(x => x.Channel == channel && x.DomainOfInfluenceType == doiType && x.Valid == valid)
            ?.CountOfReceivedVotingCards;
    }
}
