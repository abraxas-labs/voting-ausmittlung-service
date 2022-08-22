// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultBallot : PrimaryElectionResultBallot
{
    public MajorityElectionResultBundle Bundle { get; set; } = null!;

    public Guid BundleId { get; set; }

    public int IndividualVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes excl. individual votes.
    /// </summary>
    public int CandidateVoteCountExclIndividual { get; set; }

    public ICollection<MajorityElectionResultBallotCandidate> BallotCandidates { get; set; }
        = new HashSet<MajorityElectionResultBallotCandidate>();

    public ICollection<SecondaryMajorityElectionResultBallot> SecondaryMajorityElectionBallots { get; set; }
        = new HashSet<SecondaryMajorityElectionResultBallot>();
}
