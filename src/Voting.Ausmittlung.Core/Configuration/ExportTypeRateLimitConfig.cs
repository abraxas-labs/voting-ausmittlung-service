// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Configuration;

public class ExportTypeRateLimitConfig
{
    /// <summary>
    /// Gets or sets the time span in which it is checked whether a tenant has reached his <see cref="MaxExportsPerTimeSpan"/>.
    /// </summary>
    public TimeSpan TimeSpan { get; set; } = TimeSpan.FromMinutes(1);

    public int MaxExportsPerTimeSpan { get; set; } = 2;
}
