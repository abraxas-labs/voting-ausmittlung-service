// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class DoubleProportionalResultSubApportionmentLotDecision
{
    public int Number { get; set; }

    public List<DoubleProportionalResultSubApportionmentLotDecisionColumn> Columns { get; set; } = new();
}
