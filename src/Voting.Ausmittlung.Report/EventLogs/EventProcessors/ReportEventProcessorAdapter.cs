// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

internal class ReportEventProcessorAdapter<TEvent> : IReportEventProcessorAdapter
    where TEvent : IMessage<TEvent>, new()
{
    private readonly IReportEventProcessor<TEvent> _eventLogInitializer;

    public ReportEventProcessorAdapter(IReportEventProcessor<TEvent> eventLogInitializer)
    {
        _eventLogInitializer = eventLogInitializer;
    }

    public EventLog? Process(IMessage eventData, EventLogBuilderContext context)
    {
        return _eventLogInitializer.Process((TEvent)eventData, context);
    }
}
