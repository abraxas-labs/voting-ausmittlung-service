// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class VoteResultBundle : PoliticalBusinessBundle
{
    public BallotResult BallotResult { get; set; } = null!;

    public Guid BallotResultId { get; set; }

    public ICollection<VoteResultBallot> Ballots { get; set; } = new HashSet<VoteResultBallot>();

    public List<VoteResultBundleLog> Logs { get; set; } = new();
}
