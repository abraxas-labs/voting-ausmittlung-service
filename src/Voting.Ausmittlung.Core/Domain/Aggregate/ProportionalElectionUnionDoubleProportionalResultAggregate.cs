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

namespace Voting.Ausmittlung.Core.Domain.Aggregate;

public class ProportionalElectionUnionDoubleProportionalResultAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public ProportionalElectionUnionDoubleProportionalResultAggregate(IMapper mapper, EventInfoProvider eventInfoProvider)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
    }

    public Guid ProportionalElectionUnionId { get; private set; }

    public override string AggregateName => "voting-proportionalElectionUnionDoubleProportionalResult";

    public void UpdateSuperApportionmentLotDecision(
        Guid unionId,
        DoubleProportionalResultSuperApportionmentLotDecision lotDecision,
        Guid contestId,
        bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildDoubleProportionalResult(unionId, null, testingPhaseEnded);
        var ev = new ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated
        {
            DoubleProportionalResultId = Id.ToString(),
            ProportionalElectionUnionId = unionId.ToString(),
            Number = lotDecision.Number,
            Columns = { _mapper.Map<IEnumerable<ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionColumnEventData>>(lotDecision.Columns) },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    public void UpdateSubApportionmentLotDecision(
        Guid unionId,
        DoubleProportionalResultSubApportionmentLotDecision lotDecision,
        Guid contestId,
        bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildDoubleProportionalResult(unionId, null, testingPhaseEnded);
        var ev = new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated
        {
            DoubleProportionalResultId = Id.ToString(),
            ProportionalElectionUnionId = unionId.ToString(),
            Number = lotDecision.Number,
            Columns = { _mapper.Map<IEnumerable<ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionColumnEventData>>(lotDecision.Columns) },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated ev:
                Apply(ev);
                break;
            case ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated ev:
                Apply(ev);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated ev)
    {
        Id = Guid.Parse(ev.DoubleProportionalResultId);
        ProportionalElectionUnionId = Guid.Parse(ev.ProportionalElectionUnionId);
    }

    private void Apply(ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated ev)
    {
        Id = Guid.Parse(ev.DoubleProportionalResultId);
        ProportionalElectionUnionId = Guid.Parse(ev.ProportionalElectionUnionId);
    }
}
