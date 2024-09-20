// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain;

public class ProportionalElectionUnionEndResultAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;

    public ProportionalElectionUnionEndResultAggregate(EventInfoProvider eventInfoProvider)
    {
        _eventInfoProvider = eventInfoProvider;
    }

    public override string AggregateName => "voting-proportionalElectionUnionEndResult";

    public Guid ProportionalElectionUnionId { get; private set; }

    public void Finalize(Guid unionId, Guid contestId, bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, testingPhaseEnded);

        RaiseEvent(
            new ProportionalElectionUnionEndResultFinalized
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProportionalElectionUnionId = unionId.ToString(),
                ProportionalElectionUnionEndResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void RevertFinalization(Guid contestId)
    {
        RaiseEvent(
            new ProportionalElectionUnionEndResultFinalizationReverted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProportionalElectionUnionId = ProportionalElectionUnionId.ToString(),
                ProportionalElectionUnionEndResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public ActionId PrepareFinalize(Guid unionId, bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, testingPhaseEnded);
        return new ActionId(nameof(Finalize), this);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionUnionEndResultFinalized e:
                ProportionalElectionUnionId = GuidParser.Parse(e.ProportionalElectionUnionId);
                break;
            case ProportionalElectionUnionEndResultFinalizationReverted e:
                ProportionalElectionUnionId = GuidParser.Parse(e.ProportionalElectionUnionId);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }
}
