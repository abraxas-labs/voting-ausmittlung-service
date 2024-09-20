// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionUnionReportEventProcessor :
    IReportEventProcessor<ProportionalElectionUnionCreated>,
    IReportEventProcessor<ProportionalElectionUnionUpdated>,
    IReportEventProcessor<ProportionalElectionUnionDeleted>,
    IReportEventProcessor<ProportionalElectionUnionEntriesUpdated>
{
    public EventLog? Process(ProportionalElectionUnionEntriesUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ProportionalElectionUnionDeleted eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ProportionalElectionUnionUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ProportionalElectionUnionCreated eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
