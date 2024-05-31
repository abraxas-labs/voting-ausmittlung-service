// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models;

public class DoubleProportionalResultSubApportionmentLotDecision
{
    public DoubleProportionalResultSubApportionmentLotDecision(
        int number,
        IReadOnlyCollection<DoubleProportionalResultSubApportionmentLotDecisionColumn> columns)
    {
        Number = number;
        Columns = columns;
    }

    public int Number { get; }

    public IReadOnlyCollection<DoubleProportionalResultSubApportionmentLotDecisionColumn> Columns { get; }
}
