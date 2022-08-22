// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<MajorityElectionCreated>,
    IReportEventProcessor<MajorityElectionUpdated>,
    IReportEventProcessor<MajorityElectionCandidateCreated>,
    IReportEventProcessor<MajorityElectionAfterTestingPhaseUpdated>,
    IReportEventProcessor<MajorityElectionCandidateAfterTestingPhaseUpdated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionCreated eventData, EventLogBuilderContext context)
    {
        var aggregate = new MajorityElectionAggregate();
        aggregate.Apply(eventData);
        context.MajorityElectionAggregateSet.Add(aggregate);
        return null;
    }

    public EventLog? Process(MajorityElectionUpdated eventData, EventLogBuilderContext context)
    {
        var majorityElectionId = Guid.Parse(eventData.MajorityElection.Id);
        context.MajorityElectionAggregateSet.Get(majorityElectionId)?.Apply(eventData);
        return null;
    }

    public EventLog? Process(MajorityElectionAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var majorityElectionId = GuidParser.Parse(eventData.Id);
        context.MajorityElectionAggregateSet.Get(majorityElectionId)?.Apply(eventData);
        return Process(majorityElectionId, context);
    }

    public EventLog? Process(MajorityElectionCandidateCreated eventData, EventLogBuilderContext context)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElectionCandidate.MajorityElectionId);
        return ProcessAfterTestingPhaseEnded(majorityElectionId, context);
    }

    public EventLog? Process(MajorityElectionCandidateAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var majorityElectionId = Guid.Parse(eventData.MajorityElectionId);
        return Process(majorityElectionId, context);
    }
}
