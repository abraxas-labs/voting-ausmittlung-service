// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Snapper;
using Voting.Ausmittlung.Core.Utils.MajorityElectionStrategy;
using Xunit;

namespace Voting.Ausmittlung.Test.UtilsTest;

public class MajorityElectionRelativeMajorityStrategyTest : MajorityElectionMandateAlgorithmStrategyTest
{
    private readonly IMajorityElectionMandateAlgorithmStrategy _strategy = new MajorityElectionRelativeMajorityStrategy();

    [Fact]
    public void TestCandidateStates()
    {
        var meCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 2000, 1),
            ("K002", 1500, 2),
            ("K003", 1000, 3),
            ("K004", 1000, 3),
            ("K005", 750, 5),
        };

        var smeCandidates = new List<(string CandidateNumber, int VoteCount, int Rank)>
        {
            ("K001", 1000, 1),
            ("K002", 750, 2),
            ("K003", 500, 3),
            ("K004", 500, 3),
            ("K777", 500, 3),
            ("K005", 375, 5),
        };

        var endResult = BuildEndResult(
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
            ("K001", 1000, 1),
            ("K002", 750, 2),
            ("K003", 750, 2),
            ("K004", 500, 4),
            ("K777", 500, 5),
            ("K005", 375, 6),
        };

        var endResult = BuildEndResult(
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
            ("K001", 2000, 1),
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
            ("K001", 1000, 1),
            ("K777", 999, 2),
            ("K006", 800, 3),
            ("K007", 700, 4),
            ("K002", 500, 5),
            ("K003", 400, 6),
            ("K004", 300, 6),
        };

        var endResult = BuildEndResult(
            4,
            meCandidates,
            4,
            smeCandidates);

        _strategy.RecalculatePrimaryCandidateEndResultStates(endResult);
        BuildTestResult(endResult).ShouldMatchSnapshot();
    }
}
