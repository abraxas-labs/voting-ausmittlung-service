// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CountingCircleResultComment : BaseEntity
{
    public SimpleCountingCircleResult? Result { get; set; }

    public Guid ResultId { get; set; }

    public User CreatedBy { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool CreatedByMonitoringAuthority { get; set; }
}
