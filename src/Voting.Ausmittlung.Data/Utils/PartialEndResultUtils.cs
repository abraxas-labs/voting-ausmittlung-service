// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class PartialEndResultUtils
{
    public static VoteEndResult MergeIntoPartialEndResult(Vote vote, List<VoteResult> results)
    {
        var partialResult = new VoteEndResult
        {
            Vote = vote,
            VoteId = vote.Id,
            VotingCards = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.VotingCards))
                .GroupBy(vc => (vc.Channel, vc.Valid, vc.DomainOfInfluenceType))
                .Select(g => new VoteEndResultVotingCardDetail
                {
                    Channel = g.Key.Channel,
                    Valid = g.Key.Valid,
                    DomainOfInfluenceType = g.Key.DomainOfInfluenceType,
                    CountOfReceivedVotingCards = g.Sum(x => x.CountOfReceivedVotingCards),
                })
                .ToList(),
            CountOfVotersInformationSubTotals = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.CountOfVotersInformationSubTotals))
                .GroupBy(cov => (cov.Sex, cov.VoterType))
                .Select(g => new VoteEndResultCountOfVotersInformationSubTotal
                {
                    VoterType = g.Key.VoterType,
                    Sex = g.Key.Sex,
                    CountOfVoters = g.Sum(x => x.CountOfVoters),
                })
                .ToList(),
            TotalCountOfVoters = results.Sum(r => r.TotalCountOfVoters),
            CountOfDoneCountingCircles = results.Count(r => r.AuditedTentativelyTimestamp.HasValue),
            TotalCountOfCountingCircles = results.Count,
            BallotEndResults = results
                .SelectMany(r => r.Results)
                .GroupBy(r => r.BallotId)
                .Select(g => new BallotEndResult
                {
                    Ballot = g.First().Ballot,
                    BallotId = g.Key,
                    CountOfVoters = new PoliticalBusinessCountOfVoters
                    {
                        ConventionalAccountedBallots = g.Sum(r => r.CountOfVoters.ConventionalAccountedBallots ?? 0),
                        ConventionalBlankBallots = g.Sum(r => r.CountOfVoters.ConventionalBlankBallots ?? 0),
                        ConventionalInvalidBallots = g.Sum(r => r.CountOfVoters.ConventionalInvalidBallots ?? 0),
                        ConventionalReceivedBallots = g.Sum(r => r.CountOfVoters.ConventionalReceivedBallots ?? 0),
                        EVotingAccountedBallots = g.Sum(r => r.CountOfVoters.EVotingAccountedBallots),
                        EVotingBlankBallots = g.Sum(r => r.CountOfVoters.EVotingBlankBallots),
                        EVotingInvalidBallots = g.Sum(r => r.CountOfVoters.EVotingInvalidBallots),
                        EVotingReceivedBallots = g.Sum(r => r.CountOfVoters.EVotingReceivedBallots),
                    },
                    QuestionEndResults = g
                        .SelectMany(x => x.QuestionResults)
                        .GroupBy(x => x.QuestionId)
                        .Select(x => new BallotQuestionEndResult
                        {
                            Question = x.First().Question,
                            QuestionId = x.Key,
                            ConventionalSubTotal = new BallotQuestionResultSubTotal
                            {
                                TotalCountOfAnswerNo = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerNo ?? 0),
                                TotalCountOfAnswerYes = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerYes ?? 0),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerUnspecified ?? 0),
                            },
                            EVotingSubTotal = new BallotQuestionResultSubTotal
                            {
                                TotalCountOfAnswerNo = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerNo),
                                TotalCountOfAnswerYes = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerYes),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerUnspecified),
                            },
                            CountOfCountingCircleNo = x.Count(r => !r.HasMajority),
                            CountOfCountingCircleYes = x.Count(r => r.HasMajority),
                        })
                        .ToList(),
                    TieBreakQuestionEndResults = g
                        .SelectMany(x => x.TieBreakQuestionResults)
                        .GroupBy(x => x.QuestionId)
                        .Select(x => new TieBreakQuestionEndResult
                        {
                            Question = x.First().Question,
                            QuestionId = x.Key,
                            ConventionalSubTotal = new TieBreakQuestionResultSubTotal
                            {
                                TotalCountOfAnswerQ1 = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerQ1 ?? 0),
                                TotalCountOfAnswerQ2 = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerQ2 ?? 0),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.ConventionalSubTotal.TotalCountOfAnswerUnspecified ?? 0),
                            },
                            EVotingSubTotal = new TieBreakQuestionResultSubTotal
                            {
                                TotalCountOfAnswerQ1 = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerQ1),
                                TotalCountOfAnswerQ2 = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerQ2),
                                TotalCountOfAnswerUnspecified = x.Sum(r => r.EVotingSubTotal.TotalCountOfAnswerUnspecified),
                            },
                            CountOfCountingCircleQ1 = x.Count(r => r.HasQ1Majority),
                            CountOfCountingCircleQ2 = x.Count(r => r.HasQ2Majority),
                        })
                        .ToList(),
                })
                .ToList(),

            // Not enough information for this, just initialize it with the default value
            Finalized = false,
        };

        foreach (var ballotPartialResult in partialResult.BallotEndResults)
        {
            ballotPartialResult.CountOfVoters.UpdateVoterParticipation(partialResult.TotalCountOfVoters);
        }

        return partialResult;
    }

    public static MajorityElectionEndResult MergeIntoPartialEndResult(MajorityElection election, List<MajorityElectionResult> results)
    {
        var partialResult = new MajorityElectionEndResult
        {
            MajorityElection = election,
            MajorityElectionId = election.Id,
            VotingCards = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.VotingCards))
                .GroupBy(vc => (vc.Channel, vc.Valid, vc.DomainOfInfluenceType))
                .Select(g => new MajorityElectionEndResultVotingCardDetail
                {
                    Channel = g.Key.Channel,
                    Valid = g.Key.Valid,
                    DomainOfInfluenceType = g.Key.DomainOfInfluenceType,
                    CountOfReceivedVotingCards = g.Sum(x => x.CountOfReceivedVotingCards),
                })
                .ToList(),
            CountOfVotersInformationSubTotals = results
                .SelectMany(r => r.CountingCircle.ContestDetails.SelectMany(cc => cc.CountOfVotersInformationSubTotals))
                .GroupBy(cov => (cov.Sex, cov.VoterType))
                .Select(g => new MajorityElectionEndResultCountOfVotersInformationSubTotal
                {
                    VoterType = g.Key.VoterType,
                    Sex = g.Key.Sex,
                    CountOfVoters = g.Sum(x => x.CountOfVoters),
                })
                .ToList(),
            CountOfVoters = new PoliticalBusinessCountOfVoters
            {
                ConventionalAccountedBallots = results.Sum(r => r.CountOfVoters.ConventionalAccountedBallots ?? 0),
                ConventionalBlankBallots = results.Sum(r => r.CountOfVoters.ConventionalBlankBallots ?? 0),
                ConventionalInvalidBallots = results.Sum(r => r.CountOfVoters.ConventionalInvalidBallots ?? 0),
                ConventionalReceivedBallots = results.Sum(r => r.CountOfVoters.ConventionalReceivedBallots ?? 0),
                EVotingAccountedBallots = results.Sum(r => r.CountOfVoters.EVotingAccountedBallots),
                EVotingBlankBallots = results.Sum(r => r.CountOfVoters.EVotingBlankBallots),
                EVotingInvalidBallots = results.Sum(r => r.CountOfVoters.EVotingInvalidBallots),
                EVotingReceivedBallots = results.Sum(r => r.CountOfVoters.EVotingReceivedBallots),
            },
            ConventionalSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = results.Sum(r => r.ConventionalSubTotal.IndividualVoteCount ?? 0),
                InvalidVoteCount = results.Sum(r => r.ConventionalSubTotal.InvalidVoteCount ?? 0),
                EmptyVoteCountWriteIns = results.Sum(r => r.ConventionalSubTotal.EmptyVoteCountWriteIns ?? 0),
                EmptyVoteCountExclWriteIns = results.Sum(r => r.ConventionalSubTotal.EmptyVoteCountExclWriteIns ?? 0),
                TotalCandidateVoteCountExclIndividual = results.Sum(r => r.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual),
            },
            EVotingSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = results.Sum(r => r.EVotingSubTotal.IndividualVoteCount),
                InvalidVoteCount = results.Sum(r => r.EVotingSubTotal.InvalidVoteCount),
                EmptyVoteCountWriteIns = results.Sum(r => r.EVotingSubTotal.EmptyVoteCountWriteIns),
                EmptyVoteCountExclWriteIns = results.Sum(r => r.EVotingSubTotal.EmptyVoteCountExclWriteIns),
                TotalCandidateVoteCountExclIndividual = results.Sum(r => r.EVotingSubTotal.TotalCandidateVoteCountExclIndividual),
            },
            TotalCountOfVoters = results.Sum(r => r.TotalCountOfVoters),
            CountOfDoneCountingCircles = results.Count(r => r.AuditedTentativelyTimestamp.HasValue),
            TotalCountOfCountingCircles = results.Count,
            CandidateEndResults = results
                .SelectMany(r => r.CandidateResults)
                .GroupBy(r => r.CandidateId)
                .Select(g => new MajorityElectionCandidateEndResult
                {
                    Candidate = g.First().Candidate,
                    CandidateId = g.First().CandidateId,
                    State = MajorityElectionCandidateEndResultState.Pending,
                    VoteCount = g.Sum(c => c.VoteCount),
                    ConventionalVoteCount = g.Sum(c => c.ConventionalVoteCount ?? 0),
                    EVotingVoteCount = g.Sum(c => c.EVotingInclWriteInsVoteCount),
                })
                .OrderByDescending(x => x.VoteCount)
                .ThenBy(x => x.Candidate.Position)
                .ToList(),
            SecondaryMajorityElectionEndResults = election.SecondaryMajorityElections
                .Select(se => MergeIntoPartialEndResult(se, results))
                .OrderBy(x => x.SecondaryMajorityElection.PoliticalBusinessNumber)
                .ThenBy(x => x.SecondaryMajorityElection.ShortDescription)
                .ToList(),

            // Not enough information for these, just initialize them with the default value
            Finalized = false,
            Calculation = new MajorityElectionEndResultCalculation(),
        };

        partialResult.CountOfVoters.UpdateVoterParticipation(partialResult.TotalCountOfVoters);
        partialResult.OrderVotingCardsAndSubTotals();
        return partialResult;
    }

    private static SecondaryMajorityElectionEndResult MergeIntoPartialEndResult(SecondaryMajorityElection election, List<MajorityElectionResult> results)
    {
        var relevantResult = results
            .SelectMany(r => r.SecondaryMajorityElectionResults)
            .Where(ser => ser.SecondaryMajorityElectionId == election.Id)
            .ToList();
        var partialResult = new SecondaryMajorityElectionEndResult
        {
            SecondaryMajorityElection = election,
            SecondaryMajorityElectionId = election.Id,
            CandidateEndResults = relevantResult
                .SelectMany(r => r.CandidateResults)
                .GroupBy(r => r.CandidateId)
                .Select(g => new SecondaryMajorityElectionCandidateEndResult
                {
                    Candidate = g.First().Candidate,
                    CandidateId = g.First().CandidateId,
                    State = MajorityElectionCandidateEndResultState.Pending,
                    VoteCount = g.Sum(c => c.VoteCount),
                    ConventionalVoteCount = g.Sum(c => c.ConventionalVoteCount ?? 0),
                    EVotingVoteCount = g.Sum(c => c.EVotingInclWriteInsVoteCount),
                })
                .OrderByDescending(x => x.VoteCount)
                .ThenBy(x => x.Candidate.Position)
                .ToList(),
            ConventionalSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = relevantResult.Sum(r => r.ConventionalSubTotal.IndividualVoteCount ?? 0),
                InvalidVoteCount = relevantResult.Sum(r => r.ConventionalSubTotal.InvalidVoteCount ?? 0),
                EmptyVoteCountWriteIns = relevantResult.Sum(r => r.ConventionalSubTotal.EmptyVoteCountWriteIns ?? 0),
                TotalCandidateVoteCountExclIndividual = relevantResult.Sum(r => r.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual),
                EmptyVoteCountExclWriteIns = relevantResult.Sum(r => r.ConventionalSubTotal.EmptyVoteCountExclWriteIns ?? 0),
            },
            EVotingSubTotal = new MajorityElectionResultSubTotal
            {
                IndividualVoteCount = relevantResult.Sum(r => r.EVotingSubTotal.IndividualVoteCount),
                InvalidVoteCount = relevantResult.Sum(r => r.EVotingSubTotal.InvalidVoteCount),
                EmptyVoteCountWriteIns = relevantResult.Sum(r => r.EVotingSubTotal.EmptyVoteCountWriteIns),
                TotalCandidateVoteCountExclIndividual = relevantResult.Sum(r => r.EVotingSubTotal.TotalCandidateVoteCountExclIndividual),
                EmptyVoteCountExclWriteIns = relevantResult.Sum(r => r.EVotingSubTotal.EmptyVoteCountExclWriteIns),
            },
        };

        return partialResult;
    }
}
