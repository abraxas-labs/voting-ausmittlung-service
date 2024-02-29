// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;
public class ElectionGroupReportEventProcessor :
    IReportEventProcessor<ElectionGroupCreated>,
    IReportEventProcessor<ElectionGroupUpdated>,
    IReportEventProcessor<ElectionGroupDeleted>
{
    public EventLog? Process(ElectionGroupCreated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ElectionGroupUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ElectionGroupDeleted eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
