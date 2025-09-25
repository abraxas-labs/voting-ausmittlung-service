// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Configuration;

public sealed class LargeObjectHeapCompactionConfig
{
    public bool Enabled { get; set; }

    public string? PathRegex { get; set; }

    public TimeSpan PathRegexTimeout { get; set; } = TimeSpan.FromMicroseconds(100);
}
