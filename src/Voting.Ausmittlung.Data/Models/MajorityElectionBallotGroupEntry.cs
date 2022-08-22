// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionBallotGroupEntry : BaseEntity
{
    public int BlankRowCount { get; set; }

    public Guid BallotGroupId { get; set; }

    public MajorityElectionBallotGroup BallotGroup { get; set; } = null!;

    public Guid? PrimaryMajorityElectionId { get; set; }

    public MajorityElection? PrimaryMajorityElection { get; set; }

    public Guid? SecondaryMajorityElectionId { get; set; }

    public SecondaryMajorityElection? SecondaryMajorityElection { get; set; }

    public ICollection<MajorityElectionBallotGroupEntryCandidate> Candidates { get; set; } = new HashSet<MajorityElectionBallotGroupEntryCandidate>();

    public int IndividualCandidatesVoteCount { get; set; }

    public PoliticalBusiness Election => PrimaryMajorityElection as PoliticalBusiness ?? SecondaryMajorityElection!;
}
