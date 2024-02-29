// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Each row on the ballot represents one entry except for empty rows.
/// If a candidate is accumulated, this candidate has two entries.
/// </summary>
public class ProportionalElectionResultBallotCandidate : BaseEntity
{
    public ProportionalElectionCandidate Candidate { get; set; } = null!;

    public Guid CandidateId { get; set; }

    public ProportionalElectionResultBallot Ballot { get; set; } = null!;

    public Guid BallotId { get; set; }

    public int Position { get; set; }

    public bool OnList { get; set; }

    public bool RemovedFromList { get; set; }
}
