// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Core.Domain;

public class ResultExportConfiguration
{
    public Guid ContestId { get; set; }

    public Guid ExportConfigurationId { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<Guid> PoliticalBusinessIds { get; set; } = new();

    public int? IntervalMinutes { get; set; }
}
