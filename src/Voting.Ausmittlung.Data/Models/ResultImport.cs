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

    public DateTime Started { get; set; }

    public bool Completed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an import which only deleted all data and didn't import any new values.
    /// </summary>
    public bool Deleted { get; set; }

    public User StartedBy { get; set; } = new();

    public string FileName { get; set; } = string.Empty;

    public ICollection<ResultImportCountingCircle> ImportedCountingCircles { get; set; } = new HashSet<ResultImportCountingCircle>();

    public ICollection<IgnoredImportCountingCircle> IgnoredCountingCircles { get; set; } = new HashSet<IgnoredImportCountingCircle>();
}
