// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ResultImport : BaseEntity
{
    public Contest? Contest { get; set; }

    public Guid ContestId { get; set; }

    public Guid? CountingCircleId { get; set; }

    public CountingCircle? CountingCircle { get; set; }

    public DateTime Started { get; set; }

    public bool Completed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an import which only deleted all data and didn't import any new values.
    /// </summary>
    public bool Deleted { get; set; }

    public User StartedBy { get; set; } = new();

    public string FileName { get; set; } = string.Empty;

    public ResultImportType ImportType { get; set; }

    public ICollection<ResultImportCountingCircle> ImportedCountingCircles { get; set; } = new HashSet<ResultImportCountingCircle>();

    public ICollection<IgnoredImportCountingCircle> IgnoredCountingCircles { get; set; } = new HashSet<IgnoredImportCountingCircle>();

    public ICollection<MajorityElectionWriteInMapping> MajorityElectionWriteInMappings { get; set; } = new HashSet<MajorityElectionWriteInMapping>();

    public ICollection<SecondaryMajorityElectionWriteInMapping> SecondaryMajorityElectionWriteInMappings { get; set; } = new HashSet<SecondaryMajorityElectionWriteInMapping>();

    public ICollection<MajorityElectionWriteInBallot> MajorityElectionWriteInBallots { get; set; } = new HashSet<MajorityElectionWriteInBallot>();

    public ICollection<SecondaryMajorityElectionWriteInBallot> SecondaryMajorityElectionWriteInBallots { get; set; } = new HashSet<SecondaryMajorityElectionWriteInBallot>();

    public void FixImportType()
    {
        // legacy events are always evoting events.
        if (ImportType == ResultImportType.Unspecified || !CountingCircleId.HasValue)
        {
            ImportType = ResultImportType.EVoting;
        }
    }
}
