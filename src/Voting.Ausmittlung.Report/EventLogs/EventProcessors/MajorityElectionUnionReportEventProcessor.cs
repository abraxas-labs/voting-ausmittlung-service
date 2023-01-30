// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionUnionReportEventProcessor :
    IReportEventProcessor<MajorityElectionUnionCreated>,
    IReportEventProcessor<MajorityElectionUnionUpdated>,
    IReportEventProcessor<MajorityElectionUnionDeleted>,
    IReportEventProcessor<MajorityElectionUnionEntriesUpdated>
{
    public EventLog? Process(MajorityElectionUnionEntriesUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(MajorityElectionUnionDeleted eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(MajorityElectionUnionUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(MajorityElectionUnionCreated eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
