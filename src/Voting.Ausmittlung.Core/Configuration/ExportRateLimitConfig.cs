// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Configuration;

public class ExportRateLimitConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the export rate limit is disabled or not.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the clean up gap (all log entries which were created before the timespan are deleted with the clean up job).
    /// Should always be larger than the largest <see cref="ExportTypeRateLimitConfig.TimeSpan"/>.
    /// </summary>
    public TimeSpan CleanUpGap { get; set; } = new TimeSpan(2, 0, 0);

    public ExportTypeRateLimitConfig DataRateLimit { get; set; } = new();

    public ExportTypeRateLimitConfig ProtocolRateLimit { get; set; } = new();
}
