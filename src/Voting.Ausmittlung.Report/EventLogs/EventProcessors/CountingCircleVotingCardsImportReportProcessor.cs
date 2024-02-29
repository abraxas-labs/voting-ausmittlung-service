// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class CountingCircleVotingCardsImportReportProcessor : IReportEventProcessor<CountingCircleVotingCardsImported>
{
    public EventLog? Process(CountingCircleVotingCardsImported eventData, EventLogBuilderContext context)
    {
        return new();
    }
}
