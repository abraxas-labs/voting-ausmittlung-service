// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Ech.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoMajorityElectionResultMapping
{
    internal static IEnumerable<EventElectionResultDeliveryTypeElectionGroupResult> ToVoteInfoEchMajorityElectionGroupResults(
        this ICollection<MajorityElection> majorityElections,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        foreach (var majorityElection in majorityElections)
        {
            MajorityElectionResultUtils.RemoveCountToIndividualCandidatesAndAdjustTotals(majorityElection);
        }

        return majorityElections
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new EventElectionResultDeliveryTypeElectionGroupResult
            {
                ElectionGroupIdentification = x.Id.ToString(),
                ElectionResult = x.SecondaryMajorityElections
                    .OrderBy(y => y.PoliticalBusinessNumber)
                    .Select(y => ToVoteInfoEchResult(y, ctx, enabledResultStates))
                    .Prepend(ToVoteInfoEchResult(x, ctx, enabledResultStates))
                    .ToList(),
            });
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(
        MajorityElection majorityElection,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var allCountingCircleResultsArePublished = majorityElection.Results.All(x => x.Published);
        var drawElection = allCountingCircleResultsArePublished
            ? ToDrawElection(majorityElection.EndResult!, ctx)
            : null;
        var isComplete = allCountingCircleResultsArePublished
            && drawElection?.MajorityElection is not { IsDrawPending: true };

        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = majorityElection.Id.ToString(),
            Elected = allCountingCircleResultsArePublished ? new ElectedType
            {
                MajorityElection = new ElectedTypeMajorityElection
                {
                    IsElectionResultComplete = isComplete,
                    AbsoluteMajority = (uint?)majorityElection.EndResult!.Calculation.AbsoluteMajority,
                    ElectedCandidate =
                        majorityElection.EndResult.CandidateEndResults
                            .Where(x => x.State is MajorityElectionCandidateEndResultState.Elected or MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.CandidateId)
                            .Select(x => new ElectedTypeMajorityElectionElectedCandidate
                            {
                                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx),
                                IsElectedByDraw = x.LotDecision,
                            })
                            .ToList(),
                },
            }
            : null,
            CountingCircleResult = majorityElection.Results
                .OrderBy(r => r.CountingCircle.Name)
                .Select(x => ToCountingCircleResult(x.MajorityElectionId.ToString(), x, x, ToCandidateResults(x.CandidateResults, ctx), enabledResultStates))
                .ToList(),
            DrawElection = drawElection,
        };
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(
        SecondaryMajorityElection secondaryMajorityElection,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var allCountingCircleResultsArePublished = secondaryMajorityElection.Results.All(x => x.PrimaryResult.Published);
        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = secondaryMajorityElection.Id.ToString(),
            Elected = allCountingCircleResultsArePublished ? new ElectedType
            {
                MajorityElection = new ElectedTypeMajorityElection
                {
                    AbsoluteMajority = (uint?)secondaryMajorityElection.EndResult!.Calculation.AbsoluteMajority,
                    ElectedCandidate =
                        secondaryMajorityElection.EndResult.CandidateEndResults
                            .Where(x => allCountingCircleResultsArePublished && x.State is MajorityElectionCandidateEndResultState.Elected or MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.CandidateId)
                            .Select(x => new ElectedTypeMajorityElectionElectedCandidate
                            {
                                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx),
                                IsElectedByDraw = x.LotDecision,
                            })
                            .ToList(),
                },
                ProportionalElection = null,
            }
            : null,
            CountingCircleResult = secondaryMajorityElection.Results
                .OrderBy(r => r.PrimaryResult.CountingCircle.Name)
                .Select(x => ToCountingCircleResult(x.SecondaryMajorityElectionId.ToString(), x, x.PrimaryResult, ToCandidateResults(x.CandidateResults, ctx), enabledResultStates))
                .ToList(),
            DrawElection = allCountingCircleResultsArePublished ? ToDrawElection(secondaryMajorityElection.EndResult!, ctx) : null,
        };
    }

    private static CountingCircleResultType ToCountingCircleResult(
        string electionId,
        IMajorityElectionResultTotal<int> result,
        ElectionResult electionResult,
        List<CandidateResultType> candidateResults,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var resultData = electionResult.Published && enabledResultStates?.Contains(electionResult.State) != false
            ? new CountingCircleResultTypeResultData
            {
                CountOfVotersInformation = electionResult.CountingCircle.ToVoteInfoCountOfVotersInfo(electionResult.PoliticalBusiness.DomainOfInfluence),
                IsFullyCounted = electionResult.SubmissionDoneTimestamp.HasValue,
                ReleasedTimestamp = electionResult.SubmissionDoneTimestamp,
                LockoutTimestamp = electionResult.AuditedTentativelyTimestamp,
                VoterTurnout = VoteInfoCountingCircleResultMapping.DecimalToPercentage(electionResult.CountOfVoters.VoterParticipation),
                CountOfReceivedBallots = (uint)electionResult.CountOfVoters.TotalReceivedBallots,
                CountOfBlankBallots = (uint)electionResult.CountOfVoters.TotalBlankBallots,
                CountOfInvalidBallots = (uint)electionResult.CountOfVoters.TotalInvalidBallots,
                CountOfValidBallots = (uint)electionResult.CountOfVoters.TotalAccountedBallots,
                NamedElement = VoteInfoCountingCircleResultMapping.GetNamedElements(electionResult),
                ElectionResult = new ElectionResultType
                {
                    ElectionIdentification = electionId,
                    MajorityElection = new ElectionResultTypeMajorityElection
                    {
                        CountOfIndividualVotesTotal = (uint)result.IndividualVoteCount,
                        CountOfBlankVotesTotal = (uint)result.EmptyVoteCount,
                        CountOfInvalidVotesTotal = (uint)result.InvalidVoteCount,
                        CandidateResult = candidateResults,
                    },
                },
                VotingCardInformation = electionResult.CountingCircle.ToVoteInfoVotingCardInfo(electionResult.PoliticalBusiness.DomainOfInfluence.Type),
            }
            : null;
        return new CountingCircleResultType
        {
            CountingCircle = electionResult.CountingCircle.ToEch0252CountingCircle(),
            ResultData = resultData,
        };
    }

    private static DrawElectionType? ToDrawElection(MajorityElectionEndResult endResult, Ech0252MappingContext ctx)
    {
        var requiredLotDecisions = endResult.CandidateEndResults.Where(x => x.LotDecisionRequired).ToList();
        if (requiredLotDecisions.Count == 0)
        {
            return null;
        }

        var isLotDecisionApplied = requiredLotDecisions.All(x => x.LotDecision);

        return new DrawElectionType
        {
            MajorityElection = new DrawElectionTypeMajorityElection
            {
                IsDrawPending = !isLotDecisionApplied,
                CandidateDrawElection = requiredLotDecisions.ConvertAll(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx)),
                WinningCandidate = isLotDecisionApplied ? requiredLotDecisions
                    .Where(x => x.Rank <= endResult.MajorityElection.NumberOfMandates)
                    .Select(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx))
                    .ToList() : new List<CandidateOrWriteInCandidateType>(),
            },
        };
    }

    private static DrawElectionType? ToDrawElection(SecondaryMajorityElectionEndResult endResult, Ech0252MappingContext ctx)
    {
        var requiredLotDecisions = endResult.CandidateEndResults.Where(x => x.LotDecisionRequired).ToList();
        if (requiredLotDecisions.Count == 0)
        {
            return null;
        }

        var isLotDecisionApplied = requiredLotDecisions.All(x => x.LotDecision);

        return new DrawElectionType
        {
            MajorityElection = new DrawElectionTypeMajorityElection
            {
                IsDrawPending = !isLotDecisionApplied,
                CandidateDrawElection = requiredLotDecisions.ConvertAll(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx)),
                WinningCandidate = isLotDecisionApplied ? requiredLotDecisions
                    .Where(x => x.Rank <= endResult.SecondaryMajorityElection.NumberOfMandates)
                    .Select(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx))
                    .ToList() : new List<CandidateOrWriteInCandidateType>(),
            },
        };
    }

    private static CandidateOrWriteInCandidateType ToVoteInfoEchCandidateOrWriteInCandidate(this MajorityElectionCandidate candidate, Ech0252MappingContext ctx)
    {
        if (candidate.CreatedDuringActiveContest)
        {
            return new CandidateOrWriteInCandidateType
            {
                WriteInCandidate = candidate.ToVoteInfoEchCandidate(
                    ctx,
                    PoliticalBusinessType.MajorityElection,
                    candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.PartyShortDescription),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.PartyLongDescription)),
            };
        }

        return new CandidateOrWriteInCandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            CandidateReference = candidate.Number,
        };
    }

    private static CandidateOrWriteInCandidateType ToVoteInfoEchCandidateOrWriteInCandidate(this SecondaryMajorityElectionCandidate candidate, Ech0252MappingContext ctx)
    {
        if (candidate.CreatedDuringActiveContest)
        {
            return new CandidateOrWriteInCandidateType
            {
                WriteInCandidate = candidate.ToVoteInfoEchCandidate(
                    ctx,
                    PoliticalBusinessType.SecondaryMajorityElection,
                    candidate.Translations.ToDictionary(x => x.Language, x => x.OccupationTitle),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.PartyShortDescription),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.PartyLongDescription)),
            };
        }

        return new CandidateOrWriteInCandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            CandidateReference = candidate.Number,
        };
    }

    private static List<CandidateResultType> ToCandidateResults(ICollection<MajorityElectionCandidateResult> candidateResults, Ech0252MappingContext ctx)
    {
        return candidateResults
            .OrderByDescending(c => c.VoteCount)
            .ThenBy(c => c.CandidateId)
            .Select(x => new CandidateResultType
            {
                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx),
                CountOfVotesTotal = (uint)x.VoteCount,
            }).ToList();
    }

    private static List<CandidateResultType> ToCandidateResults(ICollection<SecondaryMajorityElectionCandidateResult> candidateResults, Ech0252MappingContext ctx)
    {
        return candidateResults
            .OrderByDescending(c => c.VoteCount)
            .ThenBy(c => c.CandidateId)
            .Select(x => new CandidateResultType
            {
                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(ctx),
                CountOfVotesTotal = (uint)x.VoteCount,
            }).ToList();
    }
}
