// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class MajorityElectionEndResultAggregate : BaseEventSignatureAggregate, IPoliticalBusinessEndResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public MajorityElectionEndResultAggregate(
        EventInfoProvider eventInfoProvider,
        IMapper mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-majorityElectionEndResult";

    public Guid MajorityElectionId { get; private set; }

    public ActionId PrepareFinalize(Guid politicalBusinessId, bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        return new ActionId(nameof(Finalize), this);
    }

    public void Finalize(Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded)
    {
        // We cannot check whether the aggregate is already finalized, as that state could be reset with other events (eg. lot decisions)
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        var ev = new MajorityElectionEndResultFinalized
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            MajorityElectionId = politicalBusinessId.ToString(),
            MajorityElectionEndResultId = Id.ToString(),
        };
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void RevertFinalization(Guid contestId)
    {
        // We cannot check whether the aggregate is finalized, as that state could be reset with other events (eg. lot decisions)
        RaiseEvent(
            new MajorityElectionEndResultFinalizationReverted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                MajorityElectionId = MajorityElectionId.ToString(),
                MajorityElectionEndResultId = Id.ToString(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateLotDecisions(
        Guid majorityElectionId,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions,
        Guid contestId,
        bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(majorityElectionId, testingPhaseEnded);
        RaiseEvent(
            new MajorityElectionEndResultLotDecisionsUpdated
            {
                MajorityElectionEndResultId = Id.ToString(),
                MajorityElectionId = majorityElectionId.ToString(),
                LotDecisions = { _mapper.Map<IEnumerable<MajorityElectionEndResultLotDecisionEventData>>(lotDecisions) },
                EventInfo = _eventInfoProvider.NewEventInfo(),
            },
            new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case MajorityElectionEndResultFinalized ev:
                Apply(ev);
                break;
            case MajorityElectionEndResultFinalizationReverted _: break;
            case MajorityElectionEndResultLotDecisionsUpdated ev:
                Apply(ev);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(MajorityElectionEndResultFinalized ev)
    {
        Id = Guid.Parse(ev.MajorityElectionEndResultId);
        MajorityElectionId = Guid.Parse(ev.MajorityElectionId);
    }

    private void Apply(MajorityElectionEndResultLotDecisionsUpdated ev)
    {
        Id = Guid.Parse(ev.MajorityElectionEndResultId);
        MajorityElectionId = Guid.Parse(ev.MajorityElectionId);
    }
}
