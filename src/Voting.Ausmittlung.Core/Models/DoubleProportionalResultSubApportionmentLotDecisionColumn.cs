// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class DoubleProportionalResultSubApportionmentLotDecisionColumn
{
    public DoubleProportionalResultSubApportionmentLotDecisionColumn(
        ProportionalElectionUnionList? unionList,
        ProportionalElectionList? list,
        IReadOnlyCollection<DoubleProportionalResultSubApportionmentLotDecisionCell> cells)
    {
        UnionList = unionList;
        Cells = cells;
        List = list;
    }

    public ProportionalElectionUnionList? UnionList { get; }

    public ProportionalElectionList? List { get; }

    public IReadOnlyCollection<DoubleProportionalResultSubApportionmentLotDecisionCell> Cells { get; }
}
