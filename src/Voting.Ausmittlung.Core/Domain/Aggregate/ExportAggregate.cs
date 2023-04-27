// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Proto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ExportAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public ExportAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    // Different naming convention than the other aggregates, but cannot change it due to backwards compatibility
    public override string AggregateName => "voting-ausmittlung-exports";

    internal void DataExportGenerated(
        Guid contestId,
        Guid requestId,
        string exportKey,
        Guid? countingCircleId,
        IReadOnlyCollection<Guid> politicalBusinessIds,
        DomainOfInfluenceType domainOfInfluenceType)
    {
        Id = contestId;

        RaiseEvent(
            new ExportGenerated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                Key = exportKey,
                ContestId = contestId.ToString(),
                RequestId = requestId.ToString(),
                CountingCircleId = countingCircleId?.ToString() ?? string.Empty,
                PoliticalBusinessIds = { politicalBusinessIds.Select(x => x.ToString()) },
                DomainOfInfluenceType = (Proto.DomainOfInfluenceType)domainOfInfluenceType,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void AutomatedExportTriggered(
        Guid contestId,
        Guid exportConfigurationId,
        Guid jobId,
        Proto.ResultExportTriggerMode triggerMode)
    {
        Id = contestId;

        RaiseEvent(
            new ResultExportTriggered
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ExportConfigurationId = exportConfigurationId.ToString(),
                ContestId = contestId.ToString(),
                TriggerMode = triggerMode,
                JobId = jobId.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void AutomatedExportGenerated(
        Guid contestId,
        Guid exportConfigurationId,
        Guid jobId,
        string exportKey,
        Guid? countingCircleId,
        IReadOnlyCollection<Guid> politicalBusinessIds,
        DomainOfInfluenceType domainOfInfluenceType,
        string fileName,
        string? echMessageId,
        string fileId)
    {
        RaiseEvent(
            new ResultExportGenerated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ExportConfigurationId = exportConfigurationId.ToString(),
                ContestId = contestId.ToString(),
                JobId = jobId.ToString(),
                Key = exportKey,
                FileName = fileName,
                EchMessageId = echMessageId ?? string.Empty,
                ConnectorFileId = fileId,
                CountingCircleId = countingCircleId?.ToString() ?? string.Empty,
                PoliticalBusinessIds = { politicalBusinessIds.Select(pb => pb.ToString()), },
                DomainOfInfluenceType = (Proto.DomainOfInfluenceType)domainOfInfluenceType,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void AutomatedExportCompleted(
        Guid contestId,
        Guid exportConfigurationId,
        Guid jobId,
        Proto.ResultExportTriggerMode triggerMode)
    {
        RaiseEvent(
            new ResultExportCompleted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ExportConfigurationId = exportConfigurationId.ToString(),
                ContestId = contestId.ToString(),
                TriggerMode = triggerMode,
                JobId = jobId.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void BundleReviewExported(
        Guid contestId,
        string exportKey,
        Guid countingCircleId,
        Guid politicalBusinessId,
        Guid politicalBusinessResultBundleId)
    {
        Id = contestId;

        RaiseEvent(
            new BundleReviewExportGenerated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                Key = exportKey,
                ContestId = contestId.ToString(),
                CountingCircleId = countingCircleId.ToString(),
                PoliticalBusinessId = politicalBusinessId.ToString(),
                PoliticalBusinessResultBundleId = politicalBusinessResultBundleId.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ExportGenerated ev:
                Id = Guid.Parse(ev.ContestId);
                break;
            case ResultExportTriggered ev:
                Id = Guid.Parse(ev.ContestId);
                break;
            case BundleReviewExportGenerated ev:
                Id = Guid.Parse(ev.ContestId);
                break;
            case ResultExportGenerated:
            case ResultExportCompleted:
                break;
            default:
                throw new EventNotAppliedException(eventData.GetType());
        }
    }
}