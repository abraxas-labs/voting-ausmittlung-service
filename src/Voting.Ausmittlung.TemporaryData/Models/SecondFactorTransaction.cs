// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.TemporaryData.Models;

public class SecondFactorTransaction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public int PollCount { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public string ActionIdHash { get; set; } = string.Empty;

    public List<string>? NevisExternalTokenJwtIds { get; set; }
}
