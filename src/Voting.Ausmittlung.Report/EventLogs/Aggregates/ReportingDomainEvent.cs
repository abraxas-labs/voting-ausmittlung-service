// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using EventStore.Client;
using Google.Protobuf;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Report.EventLogs.Aggregates;

public class ReportingDomainEvent : IDomainEvent
{
    public ReportingDomainEvent(Guid id, Guid aggregateId, IMessage data, IMessage? metadata, Position position, DateTime created)
    {
        Id = id;
        AggregateId = aggregateId;
        Data = data;
        Metadata = metadata;
        Position = position;
        Created = created;
    }

    public Guid Id { get; }

    public Guid AggregateId { get; }

    public IMessage Data { get; }

    public IMessage? Metadata { get; }

    public StreamRevision AggregateVersion { get; }

    public Position? Position { get; }

    public DateTime? Created { get; }
}
