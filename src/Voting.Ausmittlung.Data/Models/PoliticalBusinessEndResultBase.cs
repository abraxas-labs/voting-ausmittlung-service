// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PoliticalBusinessEndResultBase : BaseEntity
{
    /// <summary>
    /// Gets or sets the total count of voters which would have been able to make a vote.
    /// In German: Anzahl Stimmberechtigte.
    /// </summary>
    public int TotalCountOfVoters { get; set; }

    public int CountOfDoneCountingCircles { get; set; }

    public int TotalCountOfCountingCircles { get; set; }

    public bool Finalized { get; set; }

    public bool AllCountingCirclesDone => TotalCountOfCountingCircles == CountOfDoneCountingCircles
        && TotalCountOfCountingCircles != 0;
}
