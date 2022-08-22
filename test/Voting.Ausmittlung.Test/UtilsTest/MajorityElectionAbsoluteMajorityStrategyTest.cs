// (c) Copyright 2022 by Abraxas Informatik AG
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
    [InlineData(49, 49, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 25)]
    [InlineData(970, 970, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 486)]
    [InlineData(501, 500, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 251)]
    [InlineData(970, 4840, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 486)]
    [InlineData(501, 2500, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.ValidBallotsDividedByTwo, 251)]
    [InlineData(970, 970, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 486)]
    [InlineData(501, 500, 1, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 251)]
    [InlineData(970, 4840, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 485)]
    [InlineData(501, 2500, 5, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 251)]
    [InlineData(970, 1740, 2, CantonMajorityElectionAbsoluteMajorityAlgorithm.CandidateVotesDividedByTheDoubleOfNumberOfMandates, 436)] // sample from pdf "Neue Berechnung des absoluten Mehrs" of VOTING-513
    public void TestShouldReturn(
        int countOfAccountedBallots,
        int countOfCandidateVotes,
        int numberOfMandates,
        CantonMajorityElectionAbsoluteMajorityAlgorithm absoluteMajorityAlgorithm,
        int expectedResult)
    {
        var endResult = GetBasicEndResult(countOfAccountedBallots, countOfCandidateVotes, numberOfMandates, absoluteMajorityAlgorithm);
        var strategy = GetStrategy(endResult);
        strategy.CalculateAbsoluteMajority(endResult).Should().Be(expectedResult);
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
            result.MajorityElection.DomainOfInfluence.CantonDefaults.MajorityElectionAbsoluteMajorityAlgorithm,
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
                DomainOfInfluence = new DomainOfInfluence
                {
                    CantonDefaults = new DomainOfInfluenceCantonDefaults
                    {
                        MajorityElectionAbsoluteMajorityAlgorithm = absoluteMajorityAlgorithm,
                    },
                },
            },
        };
    }
}
