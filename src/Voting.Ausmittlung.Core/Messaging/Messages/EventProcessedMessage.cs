// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

public record EventProcessedMessage(
    string EventType,
    DateTime Timestamp,
    Guid? AggregateId = null,
    Guid? EntityId = null,
    Guid? ContestId = null,
    Guid? PoliticalBusinessResultBundleId = null,
    Guid? ProtocolExportId = null,
    EventProcessedMessageDetails? Details = null)
{
    public Guid? PoliticalBusinessId { get; set; }

    public Guid? PoliticalBusinessUnionId { get; set; }

    public Guid? BasisCountingCircleId { get; set; }

    public Guid? PoliticalBusinessResultId { get; set; }

    public Guid? PoliticalBusinessEndResultId { get; set; }

    public Guid? PoliticalBusinessUnionEndResultId { get; set; }
}
