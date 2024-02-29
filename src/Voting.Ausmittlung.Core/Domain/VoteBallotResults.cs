// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class VoteBallotResults
{
    public Guid BallotId { get; set; }

    public PoliticalBusinessCountOfVoters CountOfVoters { get; set; } = new();

    public List<VoteBallotQuestionResult> QuestionResults { get; set; } = new();

    public List<VoteTieBreakQuestionResult> TieBreakQuestionResults { get; set; } = new();
}
