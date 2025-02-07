// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ElectionGroupReportEventProcessor :
    IReportEventProcessor<ElectionGroupCreated>,
#pragma warning disable CS0612 // Type or member is obsolete
    IReportEventProcessor<ElectionGroupUpdated>,
#pragma warning restore CS0612 // Type or member is obsolete
    IReportEventProcessor<ElectionGroupDeleted>
{
    public EventLog? Process(ElectionGroupCreated eventData, EventLogBuilderContext context)
    {
        return new();
    }

#pragma warning disable CS0612 // Type or member is obsolete
    public EventLog? Process(ElectionGroupUpdated eventData, EventLogBuilderContext context)
#pragma warning restore CS0612 // Type or member is obsolete
    {
        return new();
    }

    public EventLog? Process(ElectionGroupDeleted eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
