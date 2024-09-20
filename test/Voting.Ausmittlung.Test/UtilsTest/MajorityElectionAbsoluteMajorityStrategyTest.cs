// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using FluentAssertions;
using Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;
using Voting.Ausmittlung.Data.Models;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class MajorityElectionAbsoluteMajorityStrategyTest
{
    private readonly MajorityElectionStrategyFactory _strategyFactory;

    public MajorityElectionAbsoluteMajorityStrategyTest()
    {
        _strategyFactory = new MajorityElectionStrategyFactory(new[]
        {
                new MajorityElectionAbsoluteMajorityStrategy(),
                new MajorityElectionAbsoluteMajorityCandidateVotesDividedByTheDoubleOfNumberOfMandatesStrategy(),
        });
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
    public void TestShouldReturn(
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
                ConventionalAccountedBallots = countOfAccountedBallots,
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
