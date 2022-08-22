// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<ProportionalElectionCreated>,
    IReportEventProcessor<ProportionalElectionUpdated>,
    IReportEventProcessor<ProportionalElectionAfterTestingPhaseUpdated>,
    IReportEventProcessor<ProportionalElectionListAfterTestingPhaseUpdated>,
    IReportEventProcessor<ProportionalElectionCandidateAfterTestingPhaseUpdated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionCreated eventData, EventLogBuilderContext context)
    {
        var aggregate = new ProportionalElectionAggregate();
        aggregate.Apply(eventData);
        context.ProportionalElectionAggregateSet.Add(aggregate);
        return null;
    }

    public EventLog? Process(ProportionalElectionUpdated eventData, EventLogBuilderContext context)
    {
        var proportionalElectionId = Guid.Parse(eventData.ProportionalElection.Id);
        context.ProportionalElectionAggregateSet.Get(proportionalElectionId)?.Apply(eventData);
        return null;
    }

    public EventLog? Process(ProportionalElectionAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var proportionalElectionId = Guid.Parse(eventData.Id);
        context.ProportionalElectionAggregateSet.Get(proportionalElectionId)?.Apply(eventData);
        return Process(proportionalElectionId, context);
    }

    public EventLog? Process(ProportionalElectionListAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var proportionalElectionId = Guid.Parse(eventData.ProportionalElectionId);
        return Process(proportionalElectionId, context);
    }

    public EventLog? Process(ProportionalElectionCandidateAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var proportionalElectionId = Guid.Parse(eventData.ProportionalElectionId);
        return Process(proportionalElectionId, context);
    }
}
