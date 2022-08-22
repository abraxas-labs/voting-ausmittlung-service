// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<VoteCreated>,
    IReportEventProcessor<VoteUpdated>,
    IReportEventProcessor<VoteAfterTestingPhaseUpdated>,
    IReportEventProcessor<BallotAfterTestingPhaseUpdated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteCreated eventData, EventLogBuilderContext context)
    {
        var aggregate = new VoteAggregate();
        aggregate.Apply(eventData);
        context.VoteAggregateSet.Add(aggregate);
        return null;
    }

    public EventLog? Process(VoteUpdated eventData, EventLogBuilderContext context)
    {
        var voteId = Guid.Parse(eventData.Vote.Id);
        context.VoteAggregateSet.Get(voteId)?.Apply(eventData);
        return null;
    }

    public EventLog? Process(VoteAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var voteId = Guid.Parse(eventData.Id);
        context.VoteAggregateSet.Get(voteId)?.Apply(eventData);
        return Process(voteId, context);
    }

    public EventLog? Process(BallotAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var voteId = Guid.Parse(eventData.VoteId);
        return Process(voteId, context);
    }
}
