// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class EventProcessingState : BaseEntity
{
    public static readonly Guid StaticId = new("b6d08709-0601-4628-b704-6aa51b1b9495");

    public EventProcessingState()
    {
        Id = StaticId;
    }

    public ulong PreparePosition { get; set; }

    public ulong CommitPosition { get; set; }

    public ulong EventNumber { get; set; }
}
