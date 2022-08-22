// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ContestCountingCircleDetailsReportEventProcessor :
    IReportEventProcessor<ContestCountingCircleDetailsCreated>,
    IReportEventProcessor<ContestCountingCircleDetailsUpdated>
{
    public EventLog? Process(ContestCountingCircleDetailsUpdated eventData, EventLogBuilderContext context)
    {
        return new() { CountingCircleId = Guid.Parse(eventData.CountingCircleId) };
    }

    public EventLog? Process(ContestCountingCircleDetailsCreated eventData, EventLogBuilderContext context)
    {
        return new() { CountingCircleId = Guid.Parse(eventData.CountingCircleId) };
    }
}
