// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionWriteInMappingBase : BaseEntity
{
    public static IEqualityComparer<string> NameComparer { get; } = StringComparer.OrdinalIgnoreCase; // write ins should be case-insensitive

    /// <summary>
    /// Gets or sets the write in name of the candidate.
    /// </summary>
    public string WriteInCandidateName { get; set; } = string.Empty;

    public int VoteCount { get; set; }

    public MajorityElectionWriteInMappingTarget Target { get; set; }

    public abstract Guid? CandidateId { get; }

    public ResultImportType ImportType { get; set; }

    public abstract Guid PoliticalBusinessId { get; }

    public Guid ImportId { get; set; }
}
