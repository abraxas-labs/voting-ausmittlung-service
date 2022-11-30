// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CountingCircle : BaseEntity, IHasSnapshotContest
{
    public string Name { get; set; } = string.Empty;

    public string Bfs { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int SortNumber { get; set; }

    public string NameForProtocol { get; set; } = string.Empty;

    public Authority ResponsibleAuthority { get; set; } = new();

    public CountingCircleContactPerson ContactPersonDuringEvent { get; set; } = new();

    public bool ContactPersonSameDuringEventAsAfter { get; set; }

    public CountingCircleContactPerson? ContactPersonAfterEvent { get; set; }

    // The id of the VOTING Basis CountingCircle
    public Guid BasisCountingCircleId { get; set; }

    // The contest id of the contest, for which this CountingCircle was "snapshotted"
    public Guid? SnapshotContestId { get; set; }

    public Contest? SnapshotContest { get; set; }

    public bool IsSnapshot => SnapshotContestId.HasValue;

    public Guid? ContestCountingCircleContactPersonId { get; set; }

    public bool MustUpdateContactPersons { get; set; }

    public ICollection<DomainOfInfluenceCountingCircle> DomainOfInfluences { get; set; } = new HashSet<DomainOfInfluenceCountingCircle>();

    public ICollection<VoteResult> VoteResults { get; set; } = new HashSet<VoteResult>();

    public ICollection<ProportionalElectionResult> ProportionalElectionResults { get; set; } = new HashSet<ProportionalElectionResult>();

    public ICollection<MajorityElectionResult> MajorityElectionResults { get; set; } = new HashSet<MajorityElectionResult>();

    public ICollection<ContestCountingCircleDetails> ContestDetails { get; set; } = new HashSet<ContestCountingCircleDetails>();

    public ICollection<SimpleCountingCircleResult> SimpleResults { get; set; } = new HashSet<SimpleCountingCircleResult>();
}
