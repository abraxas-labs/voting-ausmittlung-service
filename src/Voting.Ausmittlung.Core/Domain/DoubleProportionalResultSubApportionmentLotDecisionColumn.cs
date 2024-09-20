// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class DoubleProportionalResultSubApportionmentLotDecisionColumn
{
    public Guid UnionListId { get; set; }

    public List<DoubleProportionalResultSubApportionmentLotDecisionCell> Cells { get; set; } = new();
}
