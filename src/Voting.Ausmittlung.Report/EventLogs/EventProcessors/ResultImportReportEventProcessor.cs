﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ResultImportReportEventProcessor :
    IReportEventProcessor<ResultImportStarted>,
    IReportEventProcessor<ResultImportDataDeleted>,
    IReportEventProcessor<ResultImportCompleted>
{
    public EventLog? Process(ResultImportStarted eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ResultImportDataDeleted eventData, EventLogBuilderContext context)
    {
        return new();
    }

    public EventLog? Process(ResultImportCompleted eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
