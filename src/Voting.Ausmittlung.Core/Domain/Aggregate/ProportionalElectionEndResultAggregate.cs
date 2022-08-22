// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Google.Protobuf;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Domain;

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ProportionalElectionEndResultAggregate : BaseEventSignatureAggregate, IPoliticalBusinessEndResultAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public ProportionalElectionEndResultAggregate(
        EventInfoProvider eventInfoProvider,
        IMapper mapper,
        EventSignatureService eventSignatureService)
        : base(eventSignatureService, mapper)
    {
        _eventInfoProvider = eventInfoProvider;
        _mapper = mapper;
    }

    public override string AggregateName => "voting-proportionalElectionEndResult";

    public Guid ProportionalElectionId { get; private set; }

    public ActionId PrepareFinalize(Guid politicalBusinessId, bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        return BuildActionId(nameof(Finalize));
    }

    public void Finalize(Guid politicalBusinessId, Guid contestId, bool testingPhaseEnded)
    {
        // We cannot check whether the aggregate is already finalized, as that state could be reset with other events (eg. lot decisions)
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(politicalBusinessId, testingPhaseEnded);
        RaiseEvent(
            new ProportionalElectionEndResultFinalized
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProportionalElectionId = politicalBusinessId.ToString(),
                ProportionalElectionEndResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public void RevertFinalization(Guid contestId)
    {
        // We cannot check whether the aggregate is finalized, as that state could be reset with other events (eg. lot decisions)
        RaiseEvent(
            new ProportionalElectionEndResultFinalizationReverted
            {
                EventInfo = _eventInfoProvider.NewEventInfo(),
                ProportionalElectionId = ProportionalElectionId.ToString(),
                ProportionalElectionEndResultId = Id.ToString(),
            },
            new EventSignatureDomainData(contestId));
    }

    public void UpdateLotDecisions(
        Guid proportionalElectionId,
        Guid proportionalElectionListId,
        IEnumerable<ElectionEndResultLotDecision> lotDecisions,
        Guid contestId,
        bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(proportionalElectionId, testingPhaseEnded);
        var ev = new ProportionalElectionListEndResultLotDecisionsUpdated
        {
            ProportionalElectionEndResultId = Id.ToString(),
            ProportionalElectionId = proportionalElectionId.ToString(),
            ProportionalElectionListId = proportionalElectionListId.ToString(),
            LotDecisions = { _mapper.Map<IEnumerable<ProportionalElectionEndResultLotDecisionEventData>>(lotDecisions) },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };
        RaiseEvent(ev, new EventSignatureDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionEndResultFinalized ev:
                Apply(ev);
                break;
            case ProportionalElectionEndResultFinalizationReverted _: break;
            case ProportionalElectionListEndResultLotDecisionsUpdated ev:
                Apply(ev);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ProportionalElectionEndResultFinalized ev)
    {
        Id = Guid.Parse(ev.ProportionalElectionEndResultId);
        ProportionalElectionId = Guid.Parse(ev.ProportionalElectionId);
    }

    private void Apply(ProportionalElectionListEndResultLotDecisionsUpdated ev)
    {
        Id = Guid.Parse(ev.ProportionalElectionEndResultId);
        ProportionalElectionId = Guid.Parse(ev.ProportionalElectionId);
    }
}
