// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.EventLogs.Aggregates.Basis;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<ProportionalElectionCreated>,
    IReportEventProcessor<ProportionalElectionUpdated>,
    IReportEventProcessor<ProportionalElectionDeleted>,
    IReportEventProcessor<ProportionalElectionActiveStateUpdated>,
    IReportEventProcessor<ProportionalElectionAfterTestingPhaseUpdated>,
    IReportEventProcessor<ProportionalElectionListCreated>,
    IReportEventProcessor<ProportionalElectionListUpdated>,
    IReportEventProcessor<ProportionalElectionListDeleted>,
    IReportEventProcessor<ProportionalElectionListsReordered>,
    IReportEventProcessor<ProportionalElectionListAfterTestingPhaseUpdated>,
    IReportEventProcessor<ProportionalElectionListUnionCreated>,
    IReportEventProcessor<ProportionalElectionListUnionUpdated>,
    IReportEventProcessor<ProportionalElectionListUnionDeleted>,
    IReportEventProcessor<ProportionalElectionListUnionsReordered>,
    IReportEventProcessor<ProportionalElectionListUnionEntriesUpdated>,
    IReportEventProcessor<ProportionalElectionListUnionMainListUpdated>,
    IReportEventProcessor<ProportionalElectionCandidateCreated>,
    IReportEventProcessor<ProportionalElectionCandidateUpdated>,
    IReportEventProcessor<ProportionalElectionCandidateDeleted>,
    IReportEventProcessor<ProportionalElectionCandidatesReordered>,
    IReportEventProcessor<ProportionalElectionCandidateAfterTestingPhaseUpdated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionCreated eventData, EventLogBuilderContext context)
    {
        var aggregate = new ProportionalElectionAggregate();
        aggregate.Apply(eventData);
        context.ProportionalElectionAggregateSet.Add(aggregate);
        return Process(aggregate.Id);
    }

    public EventLog? Process(ProportionalElectionUpdated eventData, EventLogBuilderContext context)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElection.Id);
        context.ProportionalElectionAggregateSet.Get(proportionalElectionId)?.Apply(eventData);
        return Process(proportionalElectionId);
    }

    public EventLog? Process(ProportionalElectionActiveStateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.Id);
        context.ProportionalElectionAggregateSet.Get(proportionalElectionId)?.Apply(eventData);
        return Process(proportionalElectionId);
    }

    public EventLog? Process(ProportionalElectionListCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionList.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionList.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListsReordered eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUnionCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionListUnion.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUnionUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionListUnion.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUnionDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUnionsReordered eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUnionEntriesUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionListUnionEntries.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionListUnionMainListUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionCandidateCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionCandidate.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionCandidateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionCandidate.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionCandidateDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionCandidatesReordered eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionCandidateAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }
}
