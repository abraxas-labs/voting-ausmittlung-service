// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class SecondaryMajorityElectionReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<SecondaryMajorityElectionCreated>,
    IReportEventProcessor<SecondaryMajorityElectionUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionActiveStateUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionAfterTestingPhaseUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionDeleted>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateCreated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateDeleted>,
    IReportEventProcessor<SecondaryMajorityElectionCandidatesReordered>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateReferenceCreated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateReferenceUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateReferenceDeleted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.SecondaryMajorityElection;

    public EventLog? Process(SecondaryMajorityElectionCreated eventData, EventLogBuilderContext context)
    {
        context.MajorityElectionAggregateSet.Get(GuidParser.Parse(eventData.SecondaryMajorityElection.PrimaryMajorityElectionId))?.Apply(eventData);
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElection.Id));
    }

    public EventLog? Process(SecondaryMajorityElectionUpdated eventData, EventLogBuilderContext context)
    {
        context.MajorityElectionAggregateSet.Get(GuidParser.Parse(eventData.SecondaryMajorityElection.PrimaryMajorityElectionId))?.Apply(eventData);
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElection.Id));
    }

    public EventLog? Process(SecondaryMajorityElectionActiveStateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionCandidate.MajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionCandidate.MajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidatesReordered eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        context.MajorityElectionAggregateSet.Get(GuidParser.Parse(eventData.PrimaryMajorityElectionId))?.Apply(eventData);
        return Process(GuidParser.Parse(eventData.Id));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateReferenceCreated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionCandidateReference.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateReferenceUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionCandidateReference.SecondaryMajorityElectionId));
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateReferenceDeleted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.SecondaryMajorityElectionId));
    }
}
