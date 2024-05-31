// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.UtilsTest.DoubleProportional.Models;

public class DoubleProportionalAlgorithmTestInput
{
    public List<DoubleProportionalAlgorithmTestInputRow> Rows { get; set; } = new();

    public List<DoubleProportionalAlgorithmTestInputColumn> Columns { get; set; } = new();

    public ProportionalElectionMandateAlgorithm MandateAlgorithm { get; set; }

    public int?[][] VoteCounts { get; set; } = null!;
}

public class DoubleProportionalAlgorithmTestInputRow
{
    public int NumberOfMandates { get; set; }

    public string Label { get; set; } = string.Empty;
}

public class DoubleProportionalAlgorithmTestInputColumn
{
    public string Label { get; set; } = string.Empty;
}
