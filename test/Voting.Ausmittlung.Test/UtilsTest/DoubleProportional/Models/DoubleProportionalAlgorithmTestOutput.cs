// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional.Models;

public class DoubleProportionalAlgorithmTestOutput
{
    public int? CantonalQuorum { get; set; }

    public int VoteCount { get; set; }

    public int NumberOfMandates { get; set; }

    public int[]? ColumnVoteCounts { get; set; }

    public int[]? RowVoteCounts { get; set; }

    public int[]? RowQuorums { get; set; }

    public bool[]? UnionListAnyQuorumReachedList { get; set; }

    public decimal ElectionKey { get; set; }

    public decimal TotalVoterNumber { get; set; }

    public decimal[]? ColumnVoterNumbers { get; set; }

    public DoubleProportionalAlgorithmTestOutputSuperApportionment? SuperApportionment { get; set; }
}

public class DoubleProportionalAlgorithmTestOutputSuperApportionment
{
    public int NumberOfMandates { get; set; }

    public int NumberOfMandatesForLotDecision { get; set; }

    public int[]? NumberOfMandatesExclLotDecisionVector { get; set; }

    public bool[]? LotDecisionRequiredVector { get; set; }

    public int[][]? NumberOfMandatesVectors { get; set; }

    public List<DoubleProportionalAlgorithmTestOutputSubApportionment>? SubApportionments { get; set; }

    public DoubleProportionalResultApportionmentState State { get; set; }
}

public class DoubleProportionalAlgorithmTestOutputSubApportionment
{
    public int NumberOfMandates { get; set; }

    public int[][]? NumberOfMandatesExclLotDecisionMatrix { get; set; }

    public bool[][]? LotDecisionRequiredMatrix { get; set; }

    public int[][][]? NumberOfMandatesMatrices { get; set; }

    public decimal[]? RowDivisors { get; set; }

    public decimal[]? ColumnDivisors { get; set; }

    public DoubleProportionalResultApportionmentState State { get; set; }
}
