// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Subscribe;
using Voting.Lib.Messaging;
using AusmittlungEventsMetadata = Abraxas.Voting.Ausmittlung.Events.V1.Metadata;
using BasisEventsMetadata = Abraxas.Voting.Basis.Events.V1.Metadata;

namespace Voting.Ausmittlung.Core.Utils;

public class EventLogger
{
    private readonly EventProcessorContextAccessor _contextAccessor;
    private readonly MessageProducerBuffer _messageBuffer;

    public EventLogger(
        EventProcessorContextAccessor contextAccessor,
        MessageProducerBuffer messageBuffer)
    {
        _contextAccessor = contextAccessor;
        _messageBuffer = messageBuffer;
    }

    // only publish live update messages if the subscription is up to date.
    // These messages are not needed for replays/catch-ups and only result in additional load.
    // Also, these messages are not mission-critical.
    private bool MessagesDisabled => _contextAccessor.Context.IsCatchUp;

    internal void LogBundleEvent<T>(
        T eventData,
        Guid bundleId,
        Guid? politicalBusinessResultId,
        PoliticalBusinessResultBundleLog? details = null)
        where T : IMessage<T>
    {
        LogEvent(
            eventData,
            bundleId,
            bundleId,
            politicalBusinessResultId: politicalBusinessResultId,
            politicalBusinessResultBundleId: bundleId,
            details: details == null ? null : new EventProcessedMessageDetails(BundleLog: new PoliticalBusinessResultBundleLogMessageDetail(details.User, details.Timestamp, details.State)));
    }

    internal void LogResultEvent<T>(T eventData, Guid resultId)
        where T : IMessage<T>
        => LogEvent(eventData, resultId, resultId, politicalBusinessResultId: resultId);

    internal void LogEndResultEvent<T>(T eventData, Guid endResultId, Guid politicalBusinessId)
        where T : IMessage<T>
        => LogEvent(eventData, endResultId, endResultId, politicalBusinessId: politicalBusinessId, politicalBusinessEndResultId: endResultId);

    internal void LogProtocolEvent<T>(T eventData, ProtocolExport protocolExport, Guid? basisCountingCircleId = null)
        where T : IMessage<T>
    {
        var details = new ProtocolExportStateChangeEventDetail(
            protocolExport.ExportTemplateId,
            protocolExport.Id,
            protocolExport.FileName,
            protocolExport.State);
        LogEvent(
            eventData,
            protocolExport.Id,
            protocolExport.Id,
            protocolExport.ContestId,
            protocolExportId: protocolExport.Id,
            basisCountingCircleId: basisCountingCircleId ?? protocolExport.CountingCircle?.BasisCountingCircleId,
            politicalBusinessId: protocolExport.PoliticalBusinessId,
            politicalBusinessResultBundleId: protocolExport.PoliticalBusinessResultBundleId,
            details: new EventProcessedMessageDetails(ProtocolExportStateChange: details));
    }

    internal void LogEvent<T>(
        T eventData,
        Guid aggregateId,
        Guid entityId,
        Guid? contestId = null,
        Guid? politicalBusinessId = null,
        Guid? politicalBusinessResultId = null,
        Guid? politicalBusinessResultBundleId = null,
        Guid? basisCountingCircleId = null,
        Guid? protocolExportId = null,
        Guid? politicalBusinessEndResultId = null,
        EventProcessedMessageDetails? details = null)
        where T : IMessage<T>
    {
        if (MessagesDisabled)
        {
            return;
        }

        var eventInfoProp = eventData.GetType().GetProperty(nameof(EventInfo))
                            ?? throw new ArgumentException("Event has no EventInfo field", nameof(eventData));
        var eventInfo = eventInfoProp.GetValue(eventData) as EventInfo
                        ?? throw new ArgumentException("Could not retrieve event info value", nameof(eventData));

        _messageBuffer.Add(new EventProcessedMessage(
            eventData.Descriptor.FullName,
            eventInfo.Timestamp.ToDateTime(),
            aggregateId,
            entityId,
            contestId ?? GetContestId(),
            politicalBusinessResultBundleId,
            protocolExportId,
            details)
        {
            PoliticalBusinessResultId = politicalBusinessResultId,
            BasisCountingCircleId = basisCountingCircleId,
            PoliticalBusinessId = politicalBusinessId,
            PoliticalBusinessEndResultId = politicalBusinessEndResultId,
        });
    }

    private Guid GetContestId()
    {
        var metadata = _contextAccessor.Context.Event.Metadata;
        var contestId = (metadata as AusmittlungEventsMetadata.EventSignatureBusinessMetadata)?.ContestId
               ?? (metadata as BasisEventsMetadata.EventSignatureBusinessMetadata)?.ContestId;
        return GuidParser.ParseNullable(contestId) ?? Guid.Empty;
    }
}
