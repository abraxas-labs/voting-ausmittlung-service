// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.TemporaryData.Models;

public class SecondFactorTransaction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string ExternalIdentifier { get; set; } = string.Empty;

    public int PollCount { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public string ActionId { get; set; } = string.Empty;
}
