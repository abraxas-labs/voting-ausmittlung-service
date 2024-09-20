// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0110_4_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Mapping;
using Voting.Lib.Common;
using Voting.Lib.Ech;
using ProportionalElection = Voting.Ausmittlung.Data.Models.ProportionalElection;

namespace Voting.Ausmittlung.Ech.Converters;

public class Ech0110Serializer
{
    private readonly DeliveryHeaderProvider _deliveryHeaderProvider;
    private readonly IClock _clock;

    public Ech0110Serializer(IClock clock, DeliveryHeaderProvider deliveryHeaderProvider)
    {
        _clock = clock;
        _deliveryHeaderProvider = deliveryHeaderProvider;
    }

    public Delivery ToDelivery(Vote vote)
    {
        return WrapInDelivery(
            new EventResultDelivery
            {
                ReportingBody = GetReportingBody(vote.Contest),
                ContestInformation = vote.Contest.ToEchContest(),
                CountingCircleResults =
                    vote.Results.OrderBy(r => r.CountingCircle.Name)
                        .Select(vr => ToEchCountingCircleResult(vr, vote.Contest)).ToList(),
            },
            vote.Contest);
    }

    public Delivery ToDelivery(MajorityElection majorityElection)
    {
        return WrapInDelivery(
            new EventResultDelivery
            {
                ReportingBody = GetReportingBody(majorityElection.Contest),
                ContestInformation = majorityElection.Contest.ToEchContest(),
                CountingCircleResults =
                    majorityElection.Results.OrderBy(r => r.CountingCircle.Name)
                        .Select(r => ToEchCountingCircleResult(r, majorityElection.Contest)).ToList(),
            },
            majorityElection.Contest);
    }

    public Delivery ToDelivery(ProportionalElection proportionalElection)
    {
        return WrapInDelivery(
            new EventResultDelivery
            {
                ReportingBody = GetReportingBody(proportionalElection.Contest),
                ContestInformation = proportionalElection.Contest.ToEchContest(),
                CountingCircleResults =
                    proportionalElection.Results.OrderBy(r => r.CountingCircle.Name)
                        .Select(r => ToEchCountingCircleResult(r, proportionalElection.Contest)).ToList(),
            },
            proportionalElection.Contest);
    }

    private CountingCircleResultsType ToEchCountingCircleResult(MajorityElectionResult electionResult, Contest contest)
    {
        return new CountingCircleResultsType
        {
            CountingCircle = electionResult.CountingCircle.ToEchCountingCircle(),
            ElectionGroupResults = new List<ElectionGroupResultsType> { electionResult.ToEchElectionGroupResult() },
            VotingCardsInformation = ToEchVotingCardsInformation(GetCountingCircleDetails(contest, electionResult.CountingCircleId), electionResult.MajorityElection.DomainOfInfluence.Type),
        };
    }

    private CountingCircleResultsType ToEchCountingCircleResult(ProportionalElectionResult electionResult, Contest contest)
    {
        return new CountingCircleResultsType
        {
            CountingCircle = electionResult.CountingCircle.ToEchCountingCircle(),
            ElectionGroupResults = new List<ElectionGroupResultsType> { electionResult.ToEchElectionGroupResult() },
            VotingCardsInformation = ToEchVotingCardsInformation(GetCountingCircleDetails(contest, electionResult.CountingCircleId), electionResult.ProportionalElection.DomainOfInfluence.Type),
        };
    }

    private CountingCircleResultsType ToEchCountingCircleResult(VoteResult voteResult, Contest contest)
    {
        return new CountingCircleResultsType
        {
            CountingCircle = voteResult.CountingCircle.ToEchCountingCircle(),
            VoteResults = new List<VoteResultType> { voteResult.ToEchVoteResult() },
            VotingCardsInformation = ToEchVotingCardsInformation(GetCountingCircleDetails(contest, voteResult.CountingCircleId), voteResult.Vote.DomainOfInfluence.Type),
        };
    }

    private VotingCardsInformationType ToEchVotingCardsInformation(ContestCountingCircleDetails? ccDetails, DomainOfInfluenceType domainOfInfluenceType)
    {
        var countOfValidCards = ccDetails?.SumVotingCards(domainOfInfluenceType).Valid ?? 0;

        var totalCountOfVoters = ccDetails?.TotalCountOfVoters ?? 0;

        return new VotingCardsInformationType
        {
            CountOfReceivedValidVotingCardsTotal = countOfValidCards.ToString(),
            CountOfReceivedInvalidVotingCardsTotal = (totalCountOfVoters - countOfValidCards).ToString(),
        };
    }

    private ContestCountingCircleDetails? GetCountingCircleDetails(Contest contest, Guid countingCircleId)
    {
        return contest.CountingCircleDetails.FirstOrDefault(ccDetails => ccDetails.CountingCircleId == countingCircleId);
    }

    private ReportingBodyType GetReportingBody(Contest contest)
    {
        return new ReportingBodyType
        {
            ReportingBodyIdentification = contest.DomainOfInfluence.SecureConnectId,
            CreationDateTime = _clock.UtcNow,
        };
    }

    private Delivery WrapInDelivery(EventResultDelivery data, Contest contest)
    {
        return new Delivery
        {
            DeliveryHeader = _deliveryHeaderProvider.BuildHeader(!contest.TestingPhaseEnded),
            ResultDelivery = data,
        };
    }
}
