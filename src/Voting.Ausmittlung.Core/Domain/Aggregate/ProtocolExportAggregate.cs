// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Proto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ProtocolExportAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public ProtocolExportAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-protocolExport";

    public Guid ContestId { get; set; }

    public string CallbackToken { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public string ExportKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the protocol export is completed or not.
    /// </summary>
    public bool IsCompleted { get; set; }

    internal void Start(
        Guid id,
        Guid contestId,
        string fileName,
        string callbackToken,
        Guid exportTemplateId,
        Guid requestId,
        string exportKey,
        Guid? countingCircleId,
        Guid? politicalBusinessId,
        Guid? politicalBusinessUnionId,
        DomainOfInfluenceType domainOfInfluenceType,
        Guid? politicalBusinessResultBundleId)
    {
        Id = id;

        RaiseEvent(
            new ProtocolExportStarted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProtocolExportId = Id.ToString(),
                ContestId = contestId.ToString(),
                FileName = fileName,
                CallbackToken = callbackToken,
                ExportTemplateId = exportTemplateId.ToString(),

                // This is additional information, because the ExportTemplateId does not provide much info by itself
                ExportKey = exportKey,
                RequestId = requestId.ToString(),
                CountingCircleId = countingCircleId?.ToString() ?? string.Empty,
                PoliticalBusinessId = politicalBusinessId?.ToString() ?? string.Empty,
                PoliticalBusinessUnionId = politicalBusinessUnionId?.ToString() ?? string.Empty,
                DomainOfInfluenceType = (Proto.DomainOfInfluenceType)domainOfInfluenceType,
                PoliticalBusinessResultBundleId = politicalBusinessResultBundleId?.ToString() ?? string.Empty,
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    internal void Complete(string callbackToken, int printJobId)
    {
        EnsureInitialized();
        EnsureCorrectCallbackToken(callbackToken);

        RaiseEvent(
            new ProtocolExportCompleted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProtocolExportId = Id.ToString(),
                PrintJobId = printJobId,
            },
            new EventSignatureBusinessDomainData(ContestId));
    }

    internal void Fail(string callbackToken)
    {
        EnsureInitialized();
        EnsureCorrectCallbackToken(callbackToken);

        RaiseEvent(
            new ProtocolExportFailed
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProtocolExportId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(ContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProtocolExportStarted ev:
                Id = Guid.Parse(ev.ProtocolExportId);
                CallbackToken = ev.CallbackToken;
                ContestId = GuidParser.Parse(ev.ContestId);
                IsCompleted = false;
                CreatedDate = ev.EventInfo.Timestamp.ToDateTime();
                ExportKey = ev.ExportKey;
                break;
            case ProtocolExportCompleted:
                IsCompleted = true;
                break;
            case ProtocolExportFailed:
                break;
            default:
                throw new EventNotAppliedException(eventData.GetType());
        }
    }

    private void EnsureInitialized()
    {
        if (string.IsNullOrEmpty(CallbackToken) || ContestId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Aggregate not yet initialized. Call Start() before calling any other method");
        }
    }

    private void EnsureCorrectCallbackToken(string callbackToken)
    {
        // This may happen if the report generation was triggered multiple times.
        // "Older, outdated" callbacks have a token that does not match anymore.
        if (callbackToken != CallbackToken)
        {
            throw new InvalidCallbackTokenException();
        }
    }
}
