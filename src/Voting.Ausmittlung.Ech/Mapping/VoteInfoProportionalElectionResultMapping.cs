// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Extensions;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoProportionalElectionResultMapping
{
    internal static IEnumerable<EventElectionResultDeliveryTypeElectionGroupResult> ToVoteInfoEchProportionalElectionGroupResults(
        this ICollection<ProportionalElection> proportionalElections,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        return proportionalElections
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new EventElectionResultDeliveryTypeElectionGroupResult
            {
                ElectionGroupIdentification = x.Id.ToString(),
                ElectionResult =
                [
                    ToVoteInfoEchResult(x, enabledResultStates)
                ],
            });
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(
        ProportionalElection proportionalElection,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var allCountingCircleResultsArePublished = proportionalElection.Results.All(x => x.Published);
        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = proportionalElection.Id.ToString(),
            Elected = allCountingCircleResultsArePublished ? new ElectedType
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
            }
            : null,
            CountingCircleResult = proportionalElection.Results
                .OrderBy(r => r.CountingCircle.Name)
                .Select(r => ToCountingCircleResult(r, enabledResultStates))
                .ToList(),
            DrawElection = allCountingCircleResultsArePublished ? ToDrawElection(proportionalElection) : null,
        };
    }

    private static CountingCircleResultType ToCountingCircleResult(ProportionalElectionResult result, IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var resultData = result.Published && enabledResultStates?.Contains(result.State) != false
            ? new CountingCircleResultTypeResultData
            {
                CountOfVotersInformation = result.CountingCircle.ToVoteInfoCountOfVotersInfo(result.ProportionalElection.DomainOfInfluence.Type),
                IsFullyCounted = result.SubmissionDoneTimestamp.HasValue,
                ReleasedTimestamp = result.SubmissionDoneTimestamp,
                LockoutTimestamp = result.AuditedTentativelyTimestamp,
                VoterTurnout = VoteInfoCountingCircleResultMapping.DecimalToPercentage(result.CountOfVoters.VoterParticipation),
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
                            .Where(x => x.CandidateResults.Count > 0) // Otherwise it is not valid
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
                                        CandidateListResultsInfo = new ListResultTypeCandidateResultsCandidateListResultsInfo
                                        {
                                            CandidateListResults = c.ToCandidateListResults(),
                                            CountOfVotesFromBallotsWithoutListDesignation = (uint?)c.VoteSources.FirstOrDefault(y => y.ListId == null)?.VoteCount,
                                        },
                                    })
                                    .ToList(),
                            })
                            .ToList(),
                    },
                },
                VotingCardInformation = result.CountingCircle.ToVoteInfoVotingCardInfo(result.ProportionalElection.DomainOfInfluence.Type),
            }
            : null;
        return new CountingCircleResultType
        {
            CountingCircle = result.CountingCircle.ToEch0252CountingCircle(),
            ResultData = resultData,
        };
    }

    private static DrawElectionType? ToDrawElection(ProportionalElection proportionalElection)
    {
        var candidateDrawElectionOnList = ToCandidateDrawElection(proportionalElection.EndResult!);

        var doubleProportionalResult = proportionalElection.MandateAlgorithm.IsNonUnionDoubleProportional()
            ? proportionalElection.DoubleProportionalResult
            : proportionalElection.ProportionalElectionUnionEntries.FirstOrDefault()?.ProportionalElectionUnion
                .DoubleProportionalResult;

        if (proportionalElection.MandateAlgorithm.IsDoubleProportional() && doubleProportionalResult == null)
        {
            return null;
        }

        var listOrListUnionDrawElection = proportionalElection.MandateAlgorithm.IsDoubleProportional()
            ? ToDoubleProportionalDrawElection(doubleProportionalResult!, proportionalElection.Id)
            : ToProportionalListOrListUnionDrawElection(proportionalElection.EndResult!);

        if (candidateDrawElectionOnList.Count == 0 && listOrListUnionDrawElection.Count == 0)
        {
            return null;
        }

        return new DrawElectionType
        {
            ProportionalElection = new DrawElectionTypeProportionalElection
            {
                CandidateDrawElectionOnList = candidateDrawElectionOnList.Count > 0 ? candidateDrawElectionOnList : null,
                ListOrListUnionDrawElection = listOrListUnionDrawElection.Count > 0 ? listOrListUnionDrawElection : null,
            },
        };
    }

    private static List<DrawElectionTypeProportionalElectionCandidateDrawElectionOnList> ToCandidateDrawElection(
        ProportionalElectionEndResult endResult)
    {
        var candidateDrawElections = new List<DrawElectionTypeProportionalElectionCandidateDrawElectionOnList>();
        foreach (var listEndResult in endResult.ListEndResults)
        {
            var enabledCandidateEndResults = listEndResult.CandidateEndResults
                .Where(x => x.LotDecisionEnabled)
                .ToList();

            var candidateEndResultsByVoteCount = enabledCandidateEndResults
                .GroupBy(x => x.VoteCount)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var lotDecisionCandidates in candidateEndResultsByVoteCount.Values)
            {
                var isLotDecisionApplied = lotDecisionCandidates.All(y => y.LotDecision);
                candidateDrawElections.Add(new DrawElectionTypeProportionalElectionCandidateDrawElectionOnList
                {
                    ListIdentification = listEndResult.ListId.ToString(),
                    IsDrawPending = !isLotDecisionApplied,
                    CandidateIdentification = lotDecisionCandidates.ConvertAll(y => y.CandidateId.ToString()),
                    WinningCandidateIdentification = isLotDecisionApplied ? new List<string> { lotDecisionCandidates.OrderBy(x => x.Rank).First().CandidateId.ToString() } : null,
                });
            }
        }

        return candidateDrawElections;
    }

    private static List<DrawElectionTypeProportionalElectionListOrListUnionDrawElection> ToDoubleProportionalDrawElection(DoubleProportionalResult doubleProportionalResult, Guid proportionalElectionId)
    {
        var listOrListUnionDrawElections = new List<DrawElectionTypeProportionalElectionListOrListUnionDrawElection>();

        var columnsWithRequiredLotDecision = doubleProportionalResult.Columns
            .Where(x => x.SuperApportionmentLotDecisionRequired)
            .ToList();

        // super apportionment lot decision
        if (columnsWithRequiredLotDecision.Count > 0)
        {
            listOrListUnionDrawElections.Add(new DrawElectionTypeProportionalElectionListOrListUnionDrawElection
            {
                IsDrawPending = doubleProportionalResult.SuperApportionmentState == DoubleProportionalResultApportionmentState.HasOpenLotDecision,
                ListOrListUnionIdentification = columnsWithRequiredLotDecision.ConvertAll(x => new ListOrListUnionIdentificationType
                {
                    ListIdentification = x.ListId?.ToString() ?? null,
                    ListUnionIdentification = x.UnionListId?.ToString() ?? null,
                }),
                WinningListOrListUnionIdentification = columnsWithRequiredLotDecision
                    .Where(x => x.SuperApportionmentNumberOfMandatesFromLotDecision > 0)
                    .Select(x => new ListOrListUnionIdentificationType
                    {
                        ListIdentification = x.ListId?.ToString() ?? null,
                        ListUnionIdentification = x.UnionListId?.ToString() ?? null,
                    })
                    .ToList(),
            });
        }

        // sub apportionment lot decisions
        var cellsWithRequiredLotDecision = doubleProportionalResult.Columns
            .SelectMany(x => x.Cells
                .Where(y => y.SubApportionmentLotDecisionRequired && y.List.ProportionalElectionId == proportionalElectionId))
            .ToList();

        if (cellsWithRequiredLotDecision.Count == 0)
        {
            return listOrListUnionDrawElections;
        }

        listOrListUnionDrawElections.Add(new DrawElectionTypeProportionalElectionListOrListUnionDrawElection
        {
            IsDrawPending = doubleProportionalResult.SubApportionmentState == DoubleProportionalResultApportionmentState.HasOpenLotDecision,
            ListOrListUnionIdentification = cellsWithRequiredLotDecision.ConvertAll(x => new ListOrListUnionIdentificationType
            {
                ListIdentification = x.ListId.ToString(),
            }),
            WinningListOrListUnionIdentification = cellsWithRequiredLotDecision
                .Where(x => x.SubApportionmentNumberOfMandatesFromLotDecision > 0)
                .Select(x => new ListOrListUnionIdentificationType
                {
                    ListIdentification = x.ListId.ToString(),
                })
                .ToList(),
        });

        return listOrListUnionDrawElections;
    }

    private static List<DrawElectionTypeProportionalElectionListOrListUnionDrawElection> ToProportionalListOrListUnionDrawElection(ProportionalElectionEndResult endResult)
    {
        return endResult.ListLotDecisions.Select(x =>
            new DrawElectionTypeProportionalElectionListOrListUnionDrawElection
            {
                IsDrawPending = false,
                ListOrListUnionIdentification = x.Entries.Select(ToListOrListUnionIdentificationType).ToList(),
                WinningListOrListUnionIdentification = x.Entries.Where(y => y.Winning).Select(ToListOrListUnionIdentificationType).ToList(),
            })
            .ToList();
    }

    private static ListOrListUnionIdentificationType ToListOrListUnionIdentificationType(ProportionalElectionEndResultListLotDecisionEntry entry)
    {
        return entry.ListUnionId.HasValue
            ? new ListOrListUnionIdentificationType { ListUnionIdentification = entry.ListUnionId.ToString(), }
            : new ListOrListUnionIdentificationType { ListIdentification = entry.ListId.ToString(), };
    }

    private static List<CandidateListResultType> ToCandidateListResults(this ProportionalElectionCandidateResult result)
    {
        return result.VoteSources
            .Where(x => x.ListId != null)
            .OrderBy(x => x.ListId)
            .Select(x => new CandidateListResultType
            {
                ListIdentification = x.ListId.ToString(),
                CountOfVotesFromChangedBallots = (uint)x.VoteCount,
            })
            .ToList();
    }
}
