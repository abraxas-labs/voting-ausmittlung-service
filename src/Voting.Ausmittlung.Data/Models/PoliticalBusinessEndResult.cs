// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PoliticalBusinessEndResult : BaseEntity
{
    public int TotalCountOfVoters { get; set; }

    public int CountOfDoneCountingCircles { get; set; }

    public int TotalCountOfCountingCircles { get; set; }

    public bool Finalized { get; set; }

    public bool AllCountingCirclesDone => TotalCountOfCountingCircles == CountOfDoneCountingCircles
        && TotalCountOfCountingCircles != 0;
}
