// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class SecondaryMajorityElectionResultBallot : BaseEntity
{
    public MajorityElectionResultBallot PrimaryBallot { get; set; } = null!;

    public Guid PrimaryBallotId { get; set; }

    public SecondaryMajorityElectionResult SecondaryMajorityElectionResult { get; set; } = null!;

    public Guid SecondaryMajorityElectionResultId { get; set; }

    public int EmptyVoteCount { get; set; }

    public int IndividualVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    /// <summary>
    /// Gets or sets the count of candidate votes excl. individual votes.
    /// </summary>
    public int CandidateVoteCountExclIndividual { get; set; }

    public ICollection<SecondaryMajorityElectionResultBallotCandidate> BallotCandidates { get; set; }
        = new HashSet<SecondaryMajorityElectionResultBallotCandidate>();
}
