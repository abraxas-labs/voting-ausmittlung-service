// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class SimpleCountingCircleResult : BaseEntity
{
    public CountingCircle? CountingCircle { get; set; }

    public Guid CountingCircleId { get; set; }

    public SimplePoliticalBusiness? PoliticalBusiness { get; set; }

    public Guid PoliticalBusinessId { get; set; }

    public CountingCircleResultState State { get; set; } = CountingCircleResultState.Initial;

    public DateTime? SubmissionDoneTimestamp { get; set; }

    public DateTime? ReadyForCorrectionTimestamp { get; set; }

    public DateTime? AuditedTentativelyTimestamp { get; set; }

    public DateTime? PlausibilisedTimestamp { get; set; }

    public bool HasComments { get; set; }

    public ICollection<CountingCircleResultComment>? Comments { get; set; }

    public bool HasUnmappedEVotingWriteIns => CountOfElectionsWithUnmappedEVotingWriteIns > 0;

    public bool HasUnmappedECountingWriteIns => CountOfElectionsWithUnmappedECountingWriteIns > 0;

    public bool HasUnmappedWriteIns => HasUnmappedEVotingWriteIns || HasUnmappedECountingWriteIns;

    /// <summary>
    /// Gets or sets the count of elections with unmapped e-voting write ins.
    /// This includes the primary and all secondary elections.
    /// When this property is updated also update the same property in <see cref="MajorityElectionResult"/>.
    /// </summary>
    public int CountOfElectionsWithUnmappedEVotingWriteIns { get; set; }

    /// <summary>
    /// Gets or sets the count of elections with unmapped e-voting write ins.
    /// This includes the primary and all secondary elections.
    /// When this property is updated also update the same property in <see cref="MajorityElectionResult"/>.
    /// </summary>
    public int CountOfElectionsWithUnmappedECountingWriteIns { get; set; }

    public PoliticalBusinessNullableCountOfVoters CountOfVoters { get; set; } = new();

    public bool Published { get; set; }
}
