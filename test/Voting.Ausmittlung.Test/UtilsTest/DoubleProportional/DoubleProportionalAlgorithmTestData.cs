// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.UtilsTest.DoubleProportional.Models;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional;

public static class DoubleProportionalAlgorithmTestData
{
    private static readonly string SnapshotFolderPath = Path.Combine(TestSourcePaths.TestProjectSourceDirectory, "UtilsTest", "DoubleProportional", "TestFiles");

    public static ProportionalElectionUnionEndResult GenerateZhKantonratswahl2023()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-kantonratswahl-2023.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhKantonratswahl2019()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-kantonratswahl-2019.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhKantonratswahl2015()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-kantonratswahl-2015.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhGemeinderatswahl2022()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-gemeinderatswahl-2022.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateZhGemeinderatswahl2018()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("zh-gemeinderatswahl-2018.json"));
    }

    public static ProportionalElectionEndResult GenerateWinterthurStadtparlamentswahl2018()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("winterthur-stadtparlamentswahl-2018.json"));
    }

    public static ProportionalElectionEndResult GenerateWinterthurStadtparlamentswahl2022()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("winterthur-stadtparlamentswahl-2022.json"));
    }

    public static ProportionalElectionEndResult GenerateSuperApportionmentLotDecisionElectionExample()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("super-apportionment-lot-decision-election.json"));
    }

    public static ProportionalElectionEndResult GenerateSuperApportionmentMultiLotDecisionElectionExample()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("super-apportionment-multi-lot-decision-election.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateSuperApportionmentLotDecisionElectionUnionExample()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("super-apportionment-lot-decision-election-union.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateSubApportionmentLotDecisionElectionUnionExample()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("sub-apportionment-lot-decision-election-union.json"));
    }

    public static ProportionalElectionUnionEndResult GenerateSuperAndSubApportionmentLotDecisions()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateUnionEndResult(GetDoubleProportionalAlgorithmInput("super-and-sub-apportionment-lot-decisions.json"));
    }

    public static ProportionalElectionEndResult GenerateUsterStadtparlamentswahl2006()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("uster-2006.json"));
    }

    public static ProportionalElectionEndResult GenerateWetzikonStadtparlamentswahl2022()
    {
        return DoubleProportionalAlgorithmTestUtils.GenerateElectionEndResult(GetDoubleProportionalAlgorithmInput("wetzikon-2022.json"));
    }

    public static DoubleProportionalAlgorithmTestInput GetDoubleProportionalAlgorithmInput(string fileName)
    {
        return DoubleProportionalAlgorithmTestUtils.GetDoubleProportionalAlgorithmInput(Path.Combine(SnapshotFolderPath, fileName));
    }

    private record MatrixRow(string PoliticalBusinessNumber, string Name, int NumberOfMandates);

    private record MatrixCol(string OrderNumber, string Name);
}
