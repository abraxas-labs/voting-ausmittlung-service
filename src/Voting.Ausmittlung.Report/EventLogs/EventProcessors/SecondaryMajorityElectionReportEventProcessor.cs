// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class SecondaryMajorityElectionReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<SecondaryMajorityElectionCreated>,
    IReportEventProcessor<SecondaryMajorityElectionUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionAfterTestingPhaseUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateCreated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated>,
    IReportEventProcessor<SecondaryMajorityElectionCandidateReferenceCreated>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.SecondaryMajorityElection;

    public EventLog? Process(SecondaryMajorityElectionCreated eventData, EventLogBuilderContext context)
    {
        var meId = Guid.Parse(eventData.SecondaryMajorityElection.PrimaryMajorityElectionId);
        context.MajorityElectionAggregateSet.Get(meId)?.Apply(eventData);
        return null;
    }

    public EventLog? Process(SecondaryMajorityElectionUpdated eventData, EventLogBuilderContext context)
    {
        var meId = Guid.Parse(eventData.SecondaryMajorityElection.PrimaryMajorityElectionId);
        context.MajorityElectionAggregateSet.Get(meId)?.Apply(eventData);
        return null;
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateCreated eventData, EventLogBuilderContext context)
    {
        var smeId = Guid.Parse(eventData.SecondaryMajorityElectionCandidate.MajorityElectionId);
        return ProcessAfterTestingPhaseEnded(smeId, context);
    }

    public EventLog? Process(SecondaryMajorityElectionAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var meId = Guid.Parse(eventData.PrimaryMajorityElectionId);
        var smeId = Guid.Parse(eventData.Id);
        context.MajorityElectionAggregateSet.Get(meId)?.Apply(eventData);
        return Process(smeId, context);
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateAfterTestingPhaseUpdated eventData, EventLogBuilderContext context)
    {
        var smeId = Guid.Parse(eventData.SecondaryMajorityElectionId);
        return Process(smeId, context);
    }

    public EventLog? Process(SecondaryMajorityElectionCandidateReferenceCreated eventData, EventLogBuilderContext context)
    {
        var smeId = Guid.Parse(eventData.MajorityElectionCandidateReference.SecondaryMajorityElectionId);
        return ProcessAfterTestingPhaseEnded(smeId, context);
    }
}
