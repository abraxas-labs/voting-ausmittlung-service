// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionBallotGroupEntryCandidate : BaseEntity
{
    public Guid? PrimaryElectionCandidateId { get; set; }

    public MajorityElectionCandidate? PrimaryElectionCandidate { get; set; }

    public Guid? SecondaryElectionCandidateId { get; set; }

    public SecondaryMajorityElectionCandidate? SecondaryElectionCandidate { get; set; }

    public Guid BallotGroupEntryId { get; set; }

    public MajorityElectionBallotGroupEntry BallotGroupEntry { get; set; } = null!;

    public MajorityElectionCandidateBase Candidate =>
        PrimaryElectionCandidate as MajorityElectionCandidateBase ?? SecondaryElectionCandidate!;
}
