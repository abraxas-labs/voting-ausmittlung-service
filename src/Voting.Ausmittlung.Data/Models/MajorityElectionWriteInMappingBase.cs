// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionWriteInMappingBase : BaseEntity
{
    /// <summary>
    /// Gets or sets the write in name of the candidate.
    /// </summary>
    public string WriteInCandidateName { get; set; } = string.Empty;

    public int VoteCount { get; set; }

    public MajorityElectionWriteInMappingTarget Target { get; set; }

    public abstract Guid? CandidateId { get; }
}
