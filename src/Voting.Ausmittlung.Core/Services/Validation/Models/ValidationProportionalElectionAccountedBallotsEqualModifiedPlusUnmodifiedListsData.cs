// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData
{
    public int TotalAccountedBallots { get; set; }

    public int TotalCountOfBallots { get; set; }

    public int TotalCountOfUnmodifiedLists { get; set; }

    public int SumBallotsAndUnmodifiedLists { get; set; }
}
