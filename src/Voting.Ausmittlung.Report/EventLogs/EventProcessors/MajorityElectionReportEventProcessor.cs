// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<MajorityElectionCreated>,
    IReportEventProcessor<MajorityElectionUpdated>,
    IReportEventProcessor<MajorityElectionActiveStateUpdated>,
    IReportEventProcessor<MajorityElectionDeleted>,
    IReportEventProcessor<MajorityElectionCandidateCreated>,
    IReportEventProcessor<MajorityElectionCandidateUpdated>,
    IReportEventProcessor<MajorityElectionCandidatesReordered>,
    IReportEventProcessor<MajorityElectionCandidateDeleted>,
    IReportEventProcessor<MajorityElectionAfterTestingPhaseUpdated>,
    IReportEventProcessor<MajorityElectionCandidateAfterTestingPhaseUpdated>,
    IReportEventProcessor<MajorityElectionBallotGroupCreated>,
    IReportEventProcessor<MajorityElectionBallotGroupUpdated>,
    IReportEventProcessor<MajorityElectionBallotGroupDeleted>,
    IReportEventProcessor<MajorityElectionBallotGroupCandidatesUpdated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionCreated eventData, EventLogBuilderContext context)
    {
        var aggregate = new MajorityElectionAggregate();
        aggregate.Apply(eventData);
        context.MajorityElectionAggregateSet.Add(aggregate);
        return Process(aggregate.Id);
    }

    public EventLog? Process(MajorityElectionUpdated eventData, EventLogBuilderContext context)
    {
        var majorityElectionId = GuidParser.Parse(eventData.MajorityElection.Id);
        context.MajorityElectionAggregateSet.Get(majorityElectionId)?.Apply(eventData);
        return Process(majorityElectionId);
    }

    public EventLog? Process(MajorityElectionActiveStateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var majorityElectionId = GuidParser.Parse(eventData.Id);
        context.MajorityElectionAggregateSet.Get(majorityElectionId)?.Apply(eventData);
        return Process(majorityElectionId);
    }

    public EventLog? Process(MajorityElectionCandidateCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionCandidate.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionCandidateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionCandidate.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionCandidatesReordered eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionCandidateDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionCandidateAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionBallotGroupCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.BallotGroup.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionBallotGroupUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.BallotGroup.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionBallotGroupDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionBallotGroupCandidatesUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.BallotGroupCandidates.MajorityElectionId));
    }
}
