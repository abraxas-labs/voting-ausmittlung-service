﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class Ballot : BaseEntity
{
    public int Position { get; set; }

    public BallotType BallotType { get; set; }

    public bool HasTieBreakQuestions { get; set; }

    public Guid VoteId { get; set; }

    public Vote Vote { get; set; } = null!; // set by ef

    public ICollection<BallotQuestion> BallotQuestions { get; set; } = new HashSet<BallotQuestion>();

    public ICollection<TieBreakQuestion> TieBreakQuestions { get; set; } = new HashSet<TieBreakQuestion>();

    public ICollection<BallotResult> Results { get; set; } = new HashSet<BallotResult>();

    public BallotEndResult? EndResult { get; set; }

    public void OrderQuestions()
    {
        BallotQuestions = BallotQuestions
            .OrderBy(x => x.Number)
            .ToList();

        TieBreakQuestions = TieBreakQuestions
            .OrderBy(x => x.Number)
            .ToList();
    }
}
