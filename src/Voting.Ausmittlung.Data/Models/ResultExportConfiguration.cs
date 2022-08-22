// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Contest specific export configuration.
/// </summary>
public class ResultExportConfiguration : ExportConfigurationBase
{
    // no foreign key integrity to keep this entity
    // even if the original export configuration is deleted
    public Guid ExportConfigurationId { get; set; }

    public Contest? Contest { get; set; }

    public Guid ContestId { get; set; }

    public ICollection<ResultExportConfigurationPoliticalBusiness>? PoliticalBusinesses { get; set; }

    public int? IntervalMinutes { get; set; }

    public DateTime? NextExecution { get; set; }

    public void UpdateNextExecution(DateTime now)
    {
        NextExecution = !IntervalMinutes.HasValue
            ? null
            : now.AddMinutes(IntervalMinutes.Value);
    }
}
