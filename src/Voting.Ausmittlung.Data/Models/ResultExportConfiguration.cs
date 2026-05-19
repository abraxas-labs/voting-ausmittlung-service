// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// Contest specific export configuration.
/// </summary>
public class ResultExportConfiguration : BaseEntity
{
    public Guid ExportConfigurationId { get; set; }

    public ExportConfiguration? ExportConfiguration { get; set; }

    public Contest? Contest { get; set; }

    public Guid ContestId { get; set; }

    /// <summary>
    /// Gets or sets the political businesses for which exports should be generated.
    /// </summary>
    public ICollection<ResultExportConfigurationPoliticalBusiness>? PoliticalBusinesses { get; set; }

    /// <summary>
    /// Gets or sets the export metadata for the political businesses.
    /// Note that metadata may exist for political businesses that are not in <see cref="PoliticalBusinesses"/>.
    /// </summary>
    public ICollection<ResultExportConfigurationPoliticalBusinessMetadata>? PoliticalBusinessMetadata { get; set; }

    public int? IntervalMinutes { get; set; }

    public DateTime? NextExecution { get; set; }

    public DateTime? LatestExecution { get; set; }

    public void UpdateNextExecution(DateTime now)
    {
        NextExecution = !IntervalMinutes.HasValue
            ? null
            : now.AddMinutes(IntervalMinutes.Value);
    }
}
