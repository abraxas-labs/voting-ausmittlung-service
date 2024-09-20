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

    public bool HasUnmappedWriteIns => CountOfElectionsWithUnmappedWriteIns > 0;

    public int CountOfElectionsWithUnmappedWriteIns { get; set; }

    public PoliticalBusinessNullableCountOfVoters CountOfVoters { get; set; } = new();

    public bool Published { get; set; }
}
