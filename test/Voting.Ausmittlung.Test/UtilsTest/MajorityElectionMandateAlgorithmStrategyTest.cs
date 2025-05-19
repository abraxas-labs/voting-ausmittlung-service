// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.UtilsTest;

public abstract class MajorityElectionMandateAlgorithmStrategyTest
{
    protected MajorityElectionEndResult BuildEndResult(
        int meSeats,
        List<(string CandidateNumber, int VoteCount, int Rank)> meCandidates,
        int smeSeats,
        List<(string CandidateNumber, int VoteCount, int Rank)> smeCandidates)
    {
        var primaryEndResult = new MajorityElectionEndResult
        {
            CountOfDoneCountingCircles = 1,
            TotalCountOfCountingCircles = 1,
            SecondaryMajorityElectionEndResults = new List<SecondaryMajorityElectionEndResult>()
            {
                new(),
            },
        };

        var secondaryEndResult = primaryEndResult.SecondaryMajorityElectionEndResults.Single();

        primaryEndResult.MajorityElection = new() { NumberOfMandates = meSeats };
        secondaryEndResult.SecondaryMajorityElection = new() { NumberOfMandates = smeSeats };

        var meCandidatesCountAndLotDecisionByVoteCount = meCandidates
            .GroupBy(x => x.VoteCount)
            .ToDictionary(x => x.Key, x => (x.Count(), x.Select(y => y.Rank).ToHashSet().Count > 1));

        var smeCandidatesCountAndLotDecisionByVoteCount = smeCandidates
            .GroupBy(x => x.VoteCount)
            .ToDictionary(x => x.Key, x => (x.Count(), x.Select(y => y.Rank).ToHashSet().Count > 1));

        primaryEndResult.CandidateEndResults = meCandidates.ConvertAll(meCandidate =>
        {
            var candidateId = Guid.NewGuid();

            return new MajorityElectionCandidateEndResult
            {
                LotDecisionEnabled = meCandidatesCountAndLotDecisionByVoteCount[meCandidate.VoteCount].Item1 > 1,
                LotDecision = meCandidatesCountAndLotDecisionByVoteCount[meCandidate.VoteCount].Item2,
                ConventionalVoteCount = meCandidate.VoteCount,
                Rank = meCandidate.Rank,
                CandidateId = candidateId,
                Candidate = new MajorityElectionCandidate
                {
                    Id = candidateId,
                    Number = meCandidate.CandidateNumber,
                },
            };
        });

        var meCandidatesIdByCandidateNumber = primaryEndResult.CandidateEndResults
            .ToDictionary(x => x.Candidate.Number, x => (Guid?)x.Candidate.Id);

        secondaryEndResult.CandidateEndResults = smeCandidates.ConvertAll(meCandidate =>
        {
            var candidateId = Guid.NewGuid();

            return new SecondaryMajorityElectionCandidateEndResult
            {
                LotDecisionEnabled = smeCandidatesCountAndLotDecisionByVoteCount[meCandidate.VoteCount].Item1 > 1,
                LotDecision = smeCandidatesCountAndLotDecisionByVoteCount[meCandidate.VoteCount].Item2,
                ConventionalVoteCount = meCandidate.VoteCount,
                Rank = meCandidate.Rank,
                CandidateId = candidateId,
                Candidate = new SecondaryMajorityElectionCandidate
                {
                    Id = candidateId,
                    Number = meCandidate.CandidateNumber,
                    CandidateReferenceId = meCandidatesIdByCandidateNumber.GetValueOrDefault(meCandidate.CandidateNumber),
                },
            };
        });

        return primaryEndResult;
    }

    protected TestResult BuildTestResult(MajorityElectionEndResult endResult)
    {
        return new TestResult(
            endResult.CandidateEndResults
                .Select(x => new CandidateResult(x.Candidate.Number, x.VoteCount, x.Rank, x.State.ToString(), x.LotDecisionRequired))
                .ToList(),
            endResult.SecondaryMajorityElectionEndResults.Single().CandidateEndResults
                .Select(x => new CandidateResult(x.Candidate.Number, x.VoteCount, x.Rank, x.State.ToString(), x.LotDecisionRequired))
                .ToList());
    }

    protected MajorityElectionCandidateEndResult GetPrimaryCandidateEndResult(MajorityElectionEndResult endResult, string candidateNumber)
        => endResult.CandidateEndResults.Single(x => x.Candidate.Number == candidateNumber);

    protected SecondaryMajorityElectionCandidateEndResult GetSecondaryCandidateEndResult(MajorityElectionEndResult endResult, string candidateNumber)
        => endResult.SecondaryMajorityElectionEndResults.Single().CandidateEndResults.Single(x => x.Candidate.Number == candidateNumber);

    protected record TestResult(List<CandidateResult> PrimaryCandidateEndResults, List<CandidateResult> SecondaryCandidateEndResults);

    protected record CandidateResult(
        string CandidateNumber,
        int VoteCount,
        int Rank,
        string State,
        bool LotDecisionRequired);
}
