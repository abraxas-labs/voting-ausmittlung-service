// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class DoubleProportionalResultSuperApportionmentLotDecision
{
    public int Number { get; set; }

    public List<DoubleProportionalResultSuperApportionmentLotDecisionColumn> Columns { get; set; } = new();
}
