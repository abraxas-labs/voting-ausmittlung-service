// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ContestCountingCircleDetailsReportEventProcessor :
    IReportEventProcessor<ContestCountingCircleDetailsCreated>,
    IReportEventProcessor<ContestCountingCircleDetailsUpdated>,
    IReportEventProcessor<ContestCountingCircleDetailsResetted>
{
    public EventLog? Process(ContestCountingCircleDetailsUpdated eventData, EventLogBuilderContext context)
    {
        return new() { CountingCircleId = GuidParser.Parse(eventData.CountingCircleId) };
    }

    public EventLog? Process(ContestCountingCircleDetailsCreated eventData, EventLogBuilderContext context)
    {
        return new() { CountingCircleId = GuidParser.Parse(eventData.CountingCircleId) };
    }

    public EventLog? Process(ContestCountingCircleDetailsResetted eventData, EventLogBuilderContext context)
    {
        return new() { CountingCircleId = GuidParser.Parse(eventData.CountingCircleId) };
    }
}
