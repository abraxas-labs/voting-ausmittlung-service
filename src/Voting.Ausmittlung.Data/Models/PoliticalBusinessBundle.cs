// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class PoliticalBusinessBundle : BaseEntity
{
    public int Number { get; set; }

    public BallotBundleState State { get; set; }

    public User CreatedBy { get; set; } = new();

    public User? ReviewedBy { get; set; }

    public int CountOfBallots { get; set; }

    [NotMapped]
    public List<int> BallotNumbersToReview { get; set; } = new();
}
