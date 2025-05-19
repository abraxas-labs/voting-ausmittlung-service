// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoMajorityElectionResultMapping
{
    internal static IEnumerable<EventElectionResultDeliveryTypeElectionGroupResult> ToVoteInfoEchMajorityElectionGroupResults(
        this ICollection<MajorityElection> majorityElections,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        return majorityElections
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .Select(x => new EventElectionResultDeliveryTypeElectionGroupResult
            {
                ElectionGroupIdentification = x.Id.ToString(),
                ElectionResult = x.SecondaryMajorityElections
                    .OrderBy(y => y.PoliticalBusinessNumber)
                    .Select(y => ToVoteInfoEchResult(y, enabledResultStates))
                    .Prepend(ToVoteInfoEchResult(x, enabledResultStates))
                    .ToList(),
            });
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(
        MajorityElection majorityElection,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var allCountingCircleResultsArePublished = majorityElection.Results.All(x => x.Published);
        return new EventElectionResultDeliveryTypeElectionGroupResultElectionResult
        {
            ElectionIdentification = majorityElection.Id.ToString(),
            Elected = allCountingCircleResultsArePublished ? new ElectedType
            {
                MajorityElection = new ElectedTypeMajorityElection()
                {
                    AbsoluteMajority = (uint?)majorityElection.EndResult!.Calculation.AbsoluteMajority,
                    ElectedCandidate =
                        majorityElection.EndResult.CandidateEndResults
                            .Where(x => allCountingCircleResultsArePublished && x.State is MajorityElectionCandidateEndResultState.Elected or MajorityElectionCandidateEndResultState.AbsoluteMajorityAndElected)
                            .OrderBy(x => x.Rank)
                            .ThenBy(x => x.CandidateId)
                            .Select(x => new ElectedTypeMajorityElectionElectedCandidate
                            {
                                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(),
                                ElectedByDraw = x.LotDecision,
                            })
                            .ToList(),
                },
                ProportionalElection = null,
            }
            : null,
            CountingCircleResult = majorityElection.Results
                .OrderBy(r => r.CountingCircle.Name)
                .Select(x => ToCountingCircleResult(x.MajorityElectionId.ToString(), x, x, ToCandidateResults(x.CandidateResults), enabledResultStates))
                .ToList(),
            DrawElection = allCountingCircleResultsArePublished ? ToDrawElection(majorityElection.EndResult!) : null,
        };
    }

    private static EventElectionResultDeliveryTypeElectionGroupResultElectionResult ToVoteInfoEchResult(
        SecondaryMajorityElection secondaryMajorityElection,
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
                                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(),
                                ElectedByDraw = x.LotDecision,
                            })
                            .ToList(),
                },
                ProportionalElection = null,
            }
            : null,
            CountingCircleResult = secondaryMajorityElection.Results
                .OrderBy(r => r.PrimaryResult.CountingCircle.Name)
                .Select(x => ToCountingCircleResult(x.SecondaryMajorityElectionId.ToString(), x, x.PrimaryResult, ToCandidateResults(x.CandidateResults), enabledResultStates))
                .ToList(),
            DrawElection = allCountingCircleResultsArePublished ? ToDrawElection(secondaryMajorityElection.EndResult!) : null,
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
                CountOfVotersInformation = electionResult.CountingCircle.ToVoteInfoCountOfVotersInfo(electionResult.PoliticalBusiness.DomainOfInfluence.Type),
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

    private static DrawElectionType? ToDrawElection(MajorityElectionEndResult endResult)
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
                CandidateDrawElection = requiredLotDecisions.ConvertAll(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate()),
                WinningCandidate = isLotDecisionApplied ? requiredLotDecisions
                    .Where(x => x.Rank <= endResult.MajorityElection.NumberOfMandates)
                    .Select(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate())
                    .ToList() : new List<CandidateOrWriteInCandidateType>(),
            },
        };
    }

    private static DrawElectionType? ToDrawElection(SecondaryMajorityElectionEndResult endResult)
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
                CandidateDrawElection = requiredLotDecisions.ConvertAll(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate()),
                WinningCandidate = isLotDecisionApplied ? requiredLotDecisions
                    .Where(x => x.Rank <= endResult.SecondaryMajorityElection.NumberOfMandates)
                    .Select(x => x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate())
                    .ToList() : new List<CandidateOrWriteInCandidateType>(),
            },
        };
    }

    private static CandidateOrWriteInCandidateType ToVoteInfoEchCandidateOrWriteInCandidate(this MajorityElectionCandidate candidate)
    {
        if (candidate.CreatedDuringActiveContest)
        {
            return new CandidateOrWriteInCandidateType
            {
                WriteInCandidate = candidate.ToVoteInfoEchWriteInCandidate(
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Party),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Party)),
            };
        }

        return new CandidateOrWriteInCandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            CandidateReference = candidate.Number,
        };
    }

    private static CandidateOrWriteInCandidateType ToVoteInfoEchCandidateOrWriteInCandidate(this SecondaryMajorityElectionCandidate candidate)
    {
        if (candidate.CreatedDuringActiveContest)
        {
            return new CandidateOrWriteInCandidateType
            {
                WriteInCandidate = candidate.ToVoteInfoEchWriteInCandidate(
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Occupation),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Party),
                    candidate.Translations.ToDictionary(x => x.Language, x => x.Party)),
            };
        }

        return new CandidateOrWriteInCandidateType
        {
            CandidateIdentification = candidate.Id.ToString(),
            CandidateReference = candidate.Number,
        };
    }

    private static List<CandidateResultType> ToCandidateResults(ICollection<MajorityElectionCandidateResult> candidateResults)
    {
        return candidateResults
            .OrderByDescending(c => c.VoteCount)
            .ThenBy(c => c.CandidateId)
            .Select(x => new CandidateResultType
            {
                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(),
                CountOfVotesTotal = (uint)x.VoteCount,
            }).ToList();
    }

    private static List<CandidateResultType> ToCandidateResults(ICollection<SecondaryMajorityElectionCandidateResult> candidateResults)
    {
        return candidateResults
            .OrderByDescending(c => c.VoteCount)
            .ThenBy(c => c.CandidateId)
            .Select(x => new CandidateResultType
            {
                CandidateOrWriteInCandidate = x.Candidate.ToVoteInfoEchCandidateOrWriteInCandidate(),
                CountOfVotesTotal = (uint)x.VoteCount,
            }).ToList();
    }
}
