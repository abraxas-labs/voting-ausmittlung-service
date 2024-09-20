// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public class DoubleProportionalResultSuperApportionmentLotDecisionColumn
{
    public Guid? UnionListId { get; set; }

    public Guid? ListId { get; set; }

    public int NumberOfMandates { get; set; }
}
