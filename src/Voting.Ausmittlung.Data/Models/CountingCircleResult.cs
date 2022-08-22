// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class CountingCircleResult : BaseEntity
{
    public Guid CountingCircleId { get; set; }

    public CountingCircle CountingCircle { get; set; } = null!;

    public abstract PoliticalBusiness PoliticalBusiness { get; }

    public abstract Guid PoliticalBusinessId { get; set; }

    public CountingCircleResultState State { get; set; } = CountingCircleResultState.Initial;

    public DateTime? SubmissionDoneTimestamp { get; set; }

    public DateTime? AuditedTentativelyTimestamp { get; set; }

    public int TotalCountOfVoters { get; set; }

    public bool SubmissionDone()
    {
        return State >= CountingCircleResultState.SubmissionDone;
    }
}
