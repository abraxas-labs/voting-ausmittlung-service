// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using FluentAssertions;
using Snapper;
using Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;
using Voting.Ausmittlung.Data.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class MajorityElectionAbsoluteMajorityStrategyTest : MajorityElectionMandateAlgorithmStrategyTest
{
    private readonly MajorityElectionStrategyFactory _strategyFactory;
    private readonly IMajorityElectionMandateAlgorithmStrategy _strategy = new MajorityElectionAbsoluteMajorityStrategy();

    public MajorityElectionAbsoluteMajorityStrategyTest()
    {
        _strategyFactory = new MajorityElectionStrategyFactory(new[]
        {
                new MajorityElectionAbsoluteMajorityStrategy(),
                new MajorityElectionAbsoluteMajorityCandidateVotesDividedByTheDoubleOfNumberOfMandatesStrategy(),
        });
    }

    [Fact]
    public void TestCandidateStates()
    {
        var meCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 2001, 1),
            ("K002", 1500, 2),
            ("K003", 1000, 3),
            ("K004", 1000, 3),
            ("K005", 750, 5),
        };

        var smeCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 1001, 1),
            ("K002", 750, 2),
            ("K003", 500, 3),
            ("K004", 500, 3),
            ("K777", 500, 3),
            ("K005", 375, 5),
        };

        var endResult = BuildEndResult(
            4000,
            2,
            meCandidates,
            2,
            smeCandidates);

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        var result = BuildTestResult(endResult);
        result.ShouldMatchSnapshot();
    }

    [Fact]
    public void TestSeatsWithRequiredLotDecisions()
    {
        var meCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 2000, 1),
            ("K002", 1500, 2),
            ("K003", 1500, 2),
            ("K004", 1000, 4),
            ("K005", 750, 5),
        };

        var smeCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 1001, 1),
            ("K002", 750, 2),
            ("K003", 750, 2),
            ("K004", 500, 4),
            ("K777", 500, 5),
            ("K005", 375, 6),
        };

        var endResult = BuildEndResult(
            2000,
            2,
            meCandidates,
            2,
            smeCandidates);

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchChildSnapshot("AfterRecalculatePrimary");

        // Do a Lot Decision in the Primary Election.
        GetPrimaryCandidateEndResult(endResult, "K002").Rank = 3;
        GetPrimaryCandidateEndResult(endResult, "K002").LotDecision = true;
        GetPrimaryCandidateEndResult(endResult, "K003").LotDecision = true;

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchChildSnapshot("AfterSetPrimaryRequiredLotDecisions");
    }

    [Fact]
    public void TestSeatsWithPrimaryAndSecondarRequiredLotDecisions()
    {
        var meCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 2001, 1),
            ("K002", 1500, 2),
            ("K003", 1500, 2),
            ("K004", 1000, 4),
            ("K005", 750, 5),
        };

        var smeCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 1000, 1),
            ("K002", 750, 2),
            ("K003", 750, 2),
            ("K777", 750, 2),
            ("K004", 500, 5),
            ("K005", 375, 6),
        };

        var endResult = BuildEndResult(
            2000,
            2,
            meCandidates,
            2,
            smeCandidates);

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchChildSnapshot("AfterRecalculatePrimary");

        // Do a Lot Decision in the Primary Election.
        GetPrimaryCandidateEndResult(endResult, "K002").Rank = 3;
        GetPrimaryCandidateEndResult(endResult, "K002").LotDecision = true;
        GetPrimaryCandidateEndResult(endResult, "K003").LotDecision = true;

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchChildSnapshot("AfterSetPrimaryRequiredLotDecisions");

        // Do a Lot Decision in the Secondary Election.
        GetSecondaryCandidateEndResult(endResult, "K003").Rank = 3;
        GetSecondaryCandidateEndResult(endResult, "K003").LotDecision = true;
        GetSecondaryCandidateEndResult(endResult, "K777").Rank = 4;
        GetSecondaryCandidateEndResult(endResult, "K777").LotDecision = true;

        _strategy.RecalculateSecondaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchChildSnapshot("AfterSetSecondaryRequiredLotDecisions");
    }

    [Fact]
    public void TestSecondaryNotEligibleCandidates()
    {
        var meCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 2000, 1),
            ("K002", 1900, 2),
            ("K003", 1800, 3),
            ("K004", 1700, 4),
            ("K005", 1600, 5),
            ("K006", 1600, 6),
            ("K007", 1600, 7),
        };

        var smeCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 1001, 1),
            ("K777", 999, 2),
            ("K006", 800, 3),
            ("K007", 700, 4),
            ("K002", 500, 5),
            ("K003", 400, 6),
            ("K004", 300, 6),
        };

        var endResult = BuildEndResult(
            2000,
            4,
            meCandidates,
            4,
            smeCandidates);

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchSnapshot();
    }

    [Theory]
    [InlineData(49, 49, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 24.5, 25)]
    [InlineData(970, 970, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 485, 486)]
    [InlineData(501, 500, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 250.5, 251)]
    [InlineData(970, 4840, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 485, 486)]
    [InlineData(501, 2500, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 250.5, 251)]
    [InlineData(970, 970, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 485, 486)]
    [InlineData(500, 501, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 250.5, 251)]
    [InlineData(970, 4840, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 484, 485)]
    [InlineData(501, 2500, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 250, 251)]
    [InlineData(970, 1740, 2, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 435, 436)] // sample from pdf "Neue Berechnung des absoluten Mehrs" of VOTING-513
    public void TestShouldCalculateAbsoluteMajority(
        int countOfAccountedBallots,
        int countOfCandidateVotes,
        int numberOfMandates,
        CantonMajorityElectionAbsoluteMajorityAlgorithm absoluteMajorityAlgorithm,
        decimal expectedAbsoluteMajorityThreshold,
        int expectedAbsoluteMajority)
    {
        var endResult = GetBasicEndResult(countOfAccountedBallots, countOfCandidateVotes, numberOfMandates, absoluteMajorityAlgorithm);
        var strategy = GetStrategy(endResult);
        strategy.CalculateAbsoluteMajority(endResult);
        endResult.Calculation.AbsoluteMajorityThreshold.Should().Be(expectedAbsoluteMajorityThreshold);
        endResult.Calculation.AbsoluteMajority.Should().Be(expectedAbsoluteMajority);
    }

    [Fact]
    public void TestCandidateVotesDividedByTheDoubleOfNumberOfMandatesWithZeroNumberOfMandatesShouldThrow()
    {
        var endResult = GetBasicEndResult(150, 600, 0, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates);
        var strategy = GetStrategy(endResult);

        var ex = Assert.Throws<ArgumentException>(() => strategy.CalculateAbsoluteMajority(endResult));
        ex.Message.Contains("NumberOfMandates must not be 0", StringComparison.Ordinal)
            .Should().BeTrue();
    }

    private MajorityElectionAbsoluteMajorityStrategy GetStrategy(MajorityElectionEndResult result)
    {
        return (MajorityElectionAbsoluteMajorityStrategy)_strategyFactory.GetMajorityElectionMandateAlgorithmStrategy(
            result.MajorityElection.Contest.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
            result.MajorityElection.MandateAlgorithm);
    }

    private MajorityElectionEndResult BuildEndResult(
        int totalAccountedBallots,
        int meSeats,
        List<(string CandidateNumber, int VoteCount, int Rank)> meCandidates,
        int smeSeats,
        List<(string CandidateNumber, int VoteCount, int Rank)> smeCandidates)
    {
        var endResult = BuildEndResult(meSeats, meCandidates, smeSeats, smeCandidates);
        endResult.CountOfVoters = new()
        {
            ConventionalSubTotal = new PoliticalBusinessCountOfVotersSubTotal()
            {
                AccountedBallots = totalAccountedBallots,
            },
        };

        return endResult;
    }

    private MajorityElectionEndResult GetBasicEndResult(
        int countOfAccountedBallots,
        int countOfCandidateVotes,
        int numberOfMandates,
        CantonMajorityElectionAbsoluteMajorityAlgorithm absoluteMajorityAlgorithm)
    {
        var individualCandidateVoteCount = (int)(countOfCandidateVotes * .1);
        return new MajorityElectionEndResult
        {
            ConventionalSubTotal =
                {
                    TotalCandidateVoteCountExclIndividual = countOfCandidateVotes - individualCandidateVoteCount,
                    IndividualVoteCount = individualCandidateVoteCount,
                },
            CountOfVoters = new PoliticalBusinessCountOfVoters
            {
                ConventionalSubTotal = new PoliticalBusinessCountOfVotersSubTotal
                {
                    AccountedBallots = countOfAccountedBallots,
                },
            },
            MajorityElection = new MajorityElection
            {
                MandateAlgorithm = MajorityElectionMandateAlgorithm.AbsoluteMajority,
                NumberOfMandates = numberOfMandates,
                Contest = new Contest
                {
                    CantonDefaults = new ContestCantonDefaults
                    {
                        MajorityElectionAbsoluteMajorityAlgorithm = absoluteMajorityAlgorithm,
                    },
                },
            },
        };
    }
}
