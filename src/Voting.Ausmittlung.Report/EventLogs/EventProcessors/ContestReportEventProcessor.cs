// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ContestReportEventProcessor :
    IReportEventProcessor<ContestCreated>,
    IReportEventProcessor<ContestUpdated>,
    IReportEventProcessor<ContestsMerged>,
    IReportEventProcessor<ContestTestingPhaseEnded>,
    IReportEventProcessor<ContestImportStarted>,
    IReportEventProcessor<PoliticalBusinessesImportStarted>,
    IReportEventProcessor<ContestPastLocked>,
    IReportEventProcessor<ContestArchived>,
    IReportEventProcessor<ContestPastUnlocked>,
    IReportEventProcessor<ContestArchiveDateUpdated>,
#pragma warning disable CS0612 // contest counting circle options are deprecated
    IReportEventProcessor<ContestCountingCircleOptionsUpdated>
#pragma warning restore CS0612
{
    public EventLog? Process(ContestCreated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestsMerged eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestImportStarted eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(PoliticalBusinessesImportStarted eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestPastLocked eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestArchived eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestPastUnlocked eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestArchiveDateUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    [Obsolete("contest counting circle options are deprecated")]
    public EventLog? Process(ContestCountingCircleOptionsUpdated eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ContestTestingPhaseEnded eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
