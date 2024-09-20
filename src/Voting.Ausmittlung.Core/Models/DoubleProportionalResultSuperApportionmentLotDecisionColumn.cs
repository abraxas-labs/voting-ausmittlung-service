// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class DoubleProportionalResultSuperApportionmentLotDecisionColumn
{
    public DoubleProportionalResultSuperApportionmentLotDecisionColumn(
        ProportionalElectionUnionList? unionList,
        ProportionalElectionList? list,
        int numberOfMandates)
    {
        UnionList = unionList;
        List = list;
        NumberOfMandates = numberOfMandates;
    }

    public ProportionalElectionUnionList? UnionList { get; }

    public ProportionalElectionList? List { get; }

    public int NumberOfMandates { get; }
}
