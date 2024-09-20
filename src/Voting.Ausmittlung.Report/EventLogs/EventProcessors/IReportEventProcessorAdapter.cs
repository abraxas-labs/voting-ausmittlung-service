// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

internal interface IReportEventProcessorAdapter
{
    EventLog? Process(IMessage eventData, EventLogBuilderContext context);
}
