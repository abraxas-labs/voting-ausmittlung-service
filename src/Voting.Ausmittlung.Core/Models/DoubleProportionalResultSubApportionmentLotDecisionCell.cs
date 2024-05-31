// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class DoubleProportionalResultSubApportionmentLotDecisionCell
{
    public DoubleProportionalResultSubApportionmentLotDecisionCell(
        ProportionalElection election,
        ProportionalElectionList list,
        int numberOfMandates)
    {
        Election = election;
        List = list;
        NumberOfMandates = numberOfMandates;
    }

    public ProportionalElection Election { get; }

    public ProportionalElectionList List { get; }

    public int NumberOfMandates { get; }
}
