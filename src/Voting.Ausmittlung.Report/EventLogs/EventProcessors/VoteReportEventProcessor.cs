// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<VoteCreated>,
    IReportEventProcessor<VoteUpdated>,
    IReportEventProcessor<VoteActiveStateUpdated>,
    IReportEventProcessor<VoteDeleted>,
    IReportEventProcessor<VoteAfterTestingPhaseUpdated>,
    IReportEventProcessor<BallotCreated>,
    IReportEventProcessor<BallotUpdated>,
    IReportEventProcessor<BallotDeleted>,
    IReportEventProcessor<BallotAfterTestingPhaseUpdated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteCreated eventData, EventLogBuilderContext context)
    {
        var aggregate = new VoteAggregate();
        aggregate.Apply(eventData);
        context.VoteAggregateSet.Add(aggregate);
        return Process(aggregate.Id);
    }

    public EventLog? Process(VoteUpdated eventData, EventLogBuilderContext context)
    {
        var voteId = GuidParser.Parse(eventData.Vote.Id);
        context.VoteAggregateSet.Get(voteId)?.Apply(eventData);
        return Process(voteId);
    }

    public EventLog? Process(VoteAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var voteId = GuidParser.Parse(eventData.Id);
        context.VoteAggregateSet.Get(voteId)?.Apply(eventData);
        return Process(voteId);
    }

    public EventLog? Process(VoteActiveStateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.VoteId));
    }

    public EventLog? Process(VoteDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.VoteId));
    }

    public EventLog? Process(BallotAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.VoteId));
    }

    public EventLog? Process(BallotCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.Ballot.VoteId));
    }

    public EventLog? Process(BallotUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.Ballot.VoteId));
    }

    public EventLog? Process(BallotDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.VoteId));
    }
}
