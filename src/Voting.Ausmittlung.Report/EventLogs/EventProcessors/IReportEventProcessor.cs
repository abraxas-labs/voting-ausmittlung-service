// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Google.Protobuf;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public interface IReportEventProcessor<in T>
    where T : IMessage<T>
{
    /// <summary>
    /// Processes the event and builds an EventLog entry if necessary and attachs reference ids (<see cref="EventLog.CountingCircleId"/> or <see cref="EventLog.PoliticalBusinessId"/>)
    /// or other data such as <see cref="EventLog.BundleNumber"/> or <see cref="EventLog.BundleBallotNumber"/>.
    /// </summary>
    /// <param name="eventData">Event data.</param>
    /// <param name="context">Context.</param>
    /// <returns>An EventLog entry.</returns>
    EventLog? Process(T eventData, EventLogBuilderContext context);
}
