// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ContestResultImportsAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public ContestResultImportsAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-contestResultImports";

    internal Guid? LastImportId { get; private set; }

    internal void CreateImport(Guid importId, Guid contestId)
    {
        Id = contestId;
        RaiseEvent(
            new ResultImportCreated
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ContestId = Id.ToString(),
                ImportId = importId.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ResultImportCreated ev:
                Id = GuidParser.Parse(ev.ContestId);
                LastImportId = GuidParser.Parse(ev.ImportId);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }
}
