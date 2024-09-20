// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoProportionalElectionResultMapping
{
    internal static IEnumerable<EventElectionResultDeliveryTypeElectionGroupResult> ToVoteInfoEchProportionalElectionGroupResults(
        this ICollection<ProportionalElection> proportionalElections)
    {
        return proportionalElections
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new EventElectionResultDeliveryTypeElectionGroupResult
            {
                ElectionGroupIdentification = x.Id.ToString(),
                ElectionResult =
                [
                    ToVoteInfoEchResult(x)
                ],
            });
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(ProportionalElection proportionalElection)
    {
        var allCountingCircleResultsArePublished = proportionalElection.Results.All(x => x.Published);
        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = proportionalElection.Id.ToString(),
            Elected = new ElectedType
            {
                ProportionalElection = proportionalElection.EndResult!.ListEndResults
                    .OrderByDescending(x => x.TotalVoteCount)
                    .ThenBy(x => x.ListId)
                    .Select(x => new ElectedTypeProportionalElectionList
                    {
                        ListIdentification = x.ListId.ToString(),
                        CountOfSeatsGained = (uint)x.NumberOfMandates,
                        ElectedCandidate = x.CandidateEndResults
                            .Where(c => allCountingCircleResultsArePublished && c.State == ProportionalElectionCandidateEndResultState.Elected)
                            .OrderBy(c => c.Rank)
                            .ThenBy(c => c.CandidateId)
                            .Select(c => new ElectedTypeProportionalElectionListElectedCandidate
                            {
                                CandidateIdentification = c.CandidateId.ToString(),
                                ElectedByDraw = c.LotDecision,
                            })
                            .ToList(),
                    })
                    .ToList(),
            },
            CountingCircleResult = proportionalElection.Results
                .Where(r => r.Published)
                .OrderBy(r => r.CountingCircle.Name)
                .Select(ToCountingCircleResult)
                .ToList(),
        };
    }

    private static CountingCircleResultType ToCountingCircleResult(ProportionalElectionResult result)
    {
        return new CountingCircleResultType
        {
            CountOfVotersInformation = new CountOfVotersInformationType
            {
                CountOfVotersTotal = (uint)result.TotalCountOfVoters,
            },
            IsFullyCounted = result.SubmissionDoneTimestamp.HasValue,
            ReleasedTimestamp = result.SubmissionDoneTimestamp,
            LockoutTimestamp = result.AuditedTentativelyTimestamp,
            VoterTurnout = VoteInfoCountingCircleResultMapping.DecimalToPercentage(result.CountOfVoters.VoterParticipation),
            CountingCircleId = result.CountingCircle.Bfs,
            CountOfReceivedBallots = (uint)result.CountOfVoters.TotalReceivedBallots,
            CountOfBlankBallots = (uint)result.CountOfVoters.TotalBlankBallots,
            CountOfInvalidBallots = (uint)result.CountOfVoters.TotalInvalidBallots,
            CountOfValidBallots = (uint)result.CountOfVoters.TotalAccountedBallots,
            NamedElement = VoteInfoCountingCircleResultMapping.GetNamedElements(result),
            ElectionResult = new ElectionResultType
            {
                ElectionIdentification = result.ProportionalElectionId.ToString(),
                ProportionalElection = new ElectionResultTypeProportionalElection
                {
                    CountOfChangedBallotsWithoutListDesignation = (uint)result.TotalCountOfListsWithoutParty,
                    CountOfBlankVotesOfChangedBallotsWithoutListDesignation = (uint)result.TotalCountOfBlankRowsOnListsWithoutParty,
                    ListResults = result.ListResults
                        .OrderByDescending(x => x.TotalVoteCount)
                        .ThenBy(x => x.ListId)
                        .Select(x => new ListResultType
                        {
                            ListIdentification = x.ListId.ToString(),
                            CountOfUnchangedBallots = (uint)x.UnmodifiedListsCount,
                            CountOfChangedBallots = (uint)x.ModifiedListsCount,
                            CountOfAdditionalVotes = (uint)x.BlankRowsCount,
                            CountOfCandidateVotes = (uint)x.ListVotesCount,
                            CandidateResults = x.CandidateResults
                                .OrderByDescending(c => c.VoteCount)
                                .ThenBy(c => c.CandidateId)
                                .Select(c => new ListResultTypeCandidateResults
                                {
                                    CandidateIdentification = c.CandidateId.ToString(),
                                    CountOfVotesFromUnchangedBallots = (uint)c.UnmodifiedListVotesCount,
                                    CountOfVotesFromChangedBallots = (uint)c.ModifiedListVotesCount,
                                })
                                .ToList(),
                        })
                        .ToList(),
                },
            },
        };
    }
}
