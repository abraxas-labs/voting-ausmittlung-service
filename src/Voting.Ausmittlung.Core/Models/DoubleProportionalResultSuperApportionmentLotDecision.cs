// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Models;

public class DoubleProportionalResultSuperApportionmentLotDecision
{
    public DoubleProportionalResultSuperApportionmentLotDecision(
        int number,
        IReadOnlyCollection<DoubleProportionalResultSuperApportionmentLotDecisionColumn> columns)
    {
        Number = number;
        Columns = columns;
    }

    public int Number { get; }

    public IReadOnlyCollection<DoubleProportionalResultSuperApportionmentLotDecisionColumn> Columns { get; }
}
