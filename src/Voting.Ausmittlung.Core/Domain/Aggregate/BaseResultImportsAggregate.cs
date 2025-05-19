// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Diagnostics;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public abstract class BaseResultImportsAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    protected BaseResultImportsAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    internal Guid ContestId { get; private set; }

    internal Guid? CountingCircleId { get; private set; }

    internal Guid? LastImportId { get; private set; }

    internal void CreateImport(Guid resultImportsId, ResultImportAggregate resultImport)
    {
        Debug.Assert(resultImportsId != Guid.Empty, "Should never be an empty id");

        RaiseEvent(
            new ResultImportCreated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                Id = resultImportsId.ToString(),
                ContestId = resultImport.ContestId.ToString(),
                CountingCircleId = resultImport.CountingCircleId?.ToString() ?? string.Empty,
                ImportId = resultImport.Id.ToString(),
                ImportType = (Abraxas.Voting.Ausmittlung.Shared.V1.ResultImportType)resultImport.ImportType,
            },
            new EventSignatureBusinessDomainData(resultImport.ContestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ResultImportCreated ev:
                // legacy events only have the contest id field set, but not the Id field.
                Id = GuidParser.ParseNullable(ev.Id) ?? Guid.Parse(ev.ContestId);
                ContestId = GuidParser.Parse(ev.ContestId);
                CountingCircleId = GuidParser.ParseNullable(ev.CountingCircleId);
                LastImportId = GuidParser.Parse(ev.ImportId);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }
}
