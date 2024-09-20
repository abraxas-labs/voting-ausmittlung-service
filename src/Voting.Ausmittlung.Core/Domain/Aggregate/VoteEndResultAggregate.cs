// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class VoteEndResultAggregate : BaseEventSignatureAggregate, IPoliticalBusinessEndResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public VoteEndResultAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-voteEndResult";

    public Guid VoteId { get; private set; }

    public ActionId PrepareFinalize(Guid politicalBusinessId, bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        return new ActionId(nameof(Finalize), this);
    }

    public void Finalize(Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded)
    {
        // We cannot check whether the aggregate is already finalized, as that state could be reset with other events (eg. lot decisions)
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        RaiseEvent(
            new VoteEndResultFinalized
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteId = politicalBusinessId.ToString(),
                VoteEndResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void RevertFinalization(Guid contestId)
    {
        // We cannot check whether the aggregate is finalized, as that state could be reset with other events (eg. lot decisions)
        RaiseEvent(
            new VoteEndResultFinalizationReverted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                VoteId = VoteId.ToString(),
                VoteEndResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case VoteEndResultFinalized ev:
                VoteId = Guid.Parse(ev.VoteId);
                Id = Guid.Parse(ev.VoteEndResultId);
                break;
            case VoteEndResultFinalizationReverted _: break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }
}
