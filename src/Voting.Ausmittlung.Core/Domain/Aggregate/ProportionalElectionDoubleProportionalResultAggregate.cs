// (c) Copyright 2024 by Abraxas Informatik AG
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

public class ProportionalElectionDoubleProportionalResultAggregate : BaseEventSignatureAggregate
{
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IMapper _mapper;

    public ProportionalElectionDoubleProportionalResultAggregate(IMapper mapper, EventInfoProvider eventInfoProvider)
    {
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
    }

    public Guid ProportionalElectionId { get; private set; }

    public override string AggregateName => "voting-proportionalElectionDoubleProportionalResult";

    public void UpdateSuperApportionmentLotDecision(
        Guid electionId,
        DoubleProportionalResultSuperApportionmentLotDecision lotDecision,
        Guid contestId,
        bool testingPhaseEnded)
    {
        Id = AusmittlungUuidV5.BuildDoubleProportionalResult(null, electionId, testingPhaseEnded);
        var ev = new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated
        {
            DoubleProportionalResultId = Id.ToString(),
            ProportionalElectionId = electionId.ToString(),
            Number = lotDecision.Number,
            Columns = { _mapper.Map<IEnumerable<ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData>>(lotDecision.Columns) },
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };
        RaiseEvent(ev, new EventSignatureBusinessDomainData(contestId));
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated ev:
                Apply(ev);
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated ev)
    {
        Id = Guid.Parse(ev.DoubleProportionalResultId);
        ProportionalElectionId = Guid.Parse(ev.ProportionalElectionId);
    }
}
