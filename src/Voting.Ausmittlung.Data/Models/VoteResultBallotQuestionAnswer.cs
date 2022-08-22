// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class VoteResultBallotQuestionAnswer : BaseEntity
{
    public VoteResultBallot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }

    public Guid QuestionId { get; set; }

    public BallotQuestion Question { get; set; } = null!;

    public BallotQuestionAnswer Answer { get; set; }
}
