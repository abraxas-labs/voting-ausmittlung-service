﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

// counting circles should only be added to the context
// when they are needed per event log counting circle id resolving, because we have no filter otherwise.
public class CountingCircleReportEventProcessor :
    IReportEventProcessor<CountingCircleUpdated>,
    IReportEventProcessor<CountingCirclesMergerScheduleUpdated>
{
    public EventLog? Process(CountingCircleUpdated eventData, EventLogBuilderContext context)
    {
        if (context.TestingPhaseEnded)
        {
            return null;
        }

        var ccId = Guid.Parse(eventData.CountingCircle.Id);
        context.CountingCircleAggregateSet.Get(ccId)?.Apply(eventData);
        return null;
    }

    public EventLog? Process(CountingCirclesMergerScheduleUpdated eventData, EventLogBuilderContext context)
    {
        if (context.TestingPhaseEnded)
        {
            return null;
        }

        var ccId = Guid.Parse(eventData.Merger.NewCountingCircle.Id);
        context.CountingCircleAggregateSet.Get(ccId)?.Apply(eventData);
        return null;
    }
}
