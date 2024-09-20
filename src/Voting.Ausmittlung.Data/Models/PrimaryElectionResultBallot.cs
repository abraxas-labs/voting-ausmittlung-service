// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PrimaryElectionResultBallot : BaseEntity
{
    public int Number { get; set; }

    public int EmptyVoteCount { get; set; }

    public bool MarkedForReview { get; set; }
}
