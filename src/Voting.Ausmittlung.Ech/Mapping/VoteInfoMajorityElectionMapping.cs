// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoMajorityElectionMapping
{
    internal static IEnumerable<EventElectionResultDeliveryTypeElectionGroupResult> ToVoteInfoEchMajorityElectionGroups(this ICollection<MajorityElection> majorityElections)
    {
        return majorityElections
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new EventElectionResultDeliveryTypeElectionGroupResult
            {
                ElectionGroupIdentification = x.Id.ToString(),
                ElectionResult = x.SecondaryMajorityElections
                    .OrderBy(y => y.PoliticalBusinessNumber)
                    .Select(ToVoteInfoEchResult)
                    .Prepend(ToVoteInfoEchResult(x))
                    .ToList(),
            });
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(MajorityElection majorityElection)
    {
        var allCountingCircleResultsArePublished = majorityElection.Results.All(x => x.Published);
        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = majorityElection.Id.ToString(),
            Elected = new ElectedType
            {
                MajoralElection = new ElectedTypeMajoralElection
                {
                    AbsoluteMajority = (uint?)majorityElection.EndResult!.Calculation.AbsoluteMajority,
                    ElectedCandidate =
                        majorityElection.EndResult.CandidateEndResults
                            .Where(x => allCountingCircleResultsArePublished && x.State is MajorityElectionCandidateEndResultState.Elected or MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.CandidateId)
                            .Select(x => new ElectedTypeMajoralElectionElectedCandidate
                            {
                                CandidateIdentification = x.CandidateId.ToString(),
                                ElectedByDraw = x.LotDecision,
                            })
                            .ToList(),
                },
            },
            CountingCircleResult = majorityElection.Results
                .Where(r => r.Published)
                .OrderBy(r => r.CountingCircle.Name)
                .Select(x => ToCountingCircleResult(x.MajorityElectionId.ToString(), x, x, x.CandidateResults))
                .ToList(),
        };
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(SecondaryMajorityElection secondaryMajorityElection)
    {
        var allCountingCircleResultsArePublished = secondaryMajorityElection.Results.All(x => x.PrimaryResult.Published);
        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = secondaryMajorityElection.Id.ToString(),
            Elected = new ElectedType
            {
                MajoralElection = new ElectedTypeMajoralElection
                {
                    AbsoluteMajority = (uint?)secondaryMajorityElection.EndResult!.PrimaryMajorityElectionEndResult.Calculation.AbsoluteMajority,
                    ElectedCandidate =
                        secondaryMajorityElection.EndResult.CandidateEndResults
                            .Where(x => allCountingCircleResultsArePublished && x.State is MajorityElectionCandidateEndResultState.Elected or MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.CandidateId)
                            .Select(x => new ElectedTypeMajoralElectionElectedCandidate
                            {
                                CandidateIdentification = x.CandidateId.ToString(),
                                ElectedByDraw = x.LotDecision,
                            })
                            .ToList(),
                },
            },
            CountingCircleResult = secondaryMajorityElection.Results
                .Where(r => r.PrimaryResult.Published)
                .OrderBy(r => r.PrimaryResult.CountingCircle.Name)
                .Select(x => ToCountingCircleResult(x.SecondaryMajorityElectionId.ToString(), x, x.PrimaryResult, x.CandidateResults))
                .ToList(),
        };
    }

    private static CountingCircleResultType ToCountingCircleResult(
        string electionId,
        IMajorityElectionResultTotal<int> result,
        ElectionResult electionResult,
        IEnumerable<MajorityElectionCandidateResultBase> candidateResults)
    {
        return new CountingCircleResultType
        {
            CountOfVotersInformation = new CountOfVotersInformationType
            {
                CountOfVotersTotal = (uint)electionResult.TotalCountOfVoters,
            },
            FullyCountedTrue = electionResult.SubmissionDoneTimestamp.HasValue,
            ReleasedTimestamp = electionResult.SubmissionDoneTimestamp,
            LockoutTimestamp = electionResult.AuditedTentativelyTimestamp,
            VoterTurnout = electionResult.CountOfVoters.VoterParticipation,
            CountingCircleId = electionResult.CountingCircle.BasisCountingCircleId.ToString(),
            CountOfReceivedBallots = (uint)electionResult.CountOfVoters.TotalReceivedBallots,
            CountOfBlankBallots = (uint)electionResult.CountOfVoters.TotalBlankBallots,
            CountOfInvalidBallots = (uint)electionResult.CountOfVoters.TotalInvalidBallots,
            CountOfValidBallots = (uint)electionResult.CountOfVoters.TotalAccountedBallots,
            ElectionResult = new ElectionResultType
            {
                ElectionIdentification = electionId,
                MajoralElection = new ElectionResultTypeMajoralElection
                {
                    CountOfIndividualVotesTotal = (uint)result.IndividualVoteCount,
                    CountOfBlankVotesTotal = (uint)result.EmptyVoteCount,
                    CountOfInvalidVotesTotal = (uint)result.InvalidVoteCount,
                    CandidateResult = candidateResults
                        .OrderByDescending(c => c.VoteCount)
                        .ThenBy(c => c.CandidateId)
                        .Select(x => new CandidateResultType
                        {
                            CandidateIdentification = x.CandidateId.ToString(),
                            CountOfVotesTotal = (uint)x.VoteCount,
                        }).ToList(),
                },
            },
        };
    }
}
