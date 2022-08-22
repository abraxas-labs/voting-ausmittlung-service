// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Report.EventLogs.Aggregates;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public abstract class BasePoliticalBusinessResultBundleReportEventProcessor : BaseCountingCircleResultReportEventProcessor
{
    public EventLog? ProcessBundleCreated(Guid id, Guid resultId, int bundleNumber, EventLogBuilderContext context)
    {
        var bundle = new PoliticalBusinessResultBundleAggregate(id, resultId, bundleNumber);
        context.PoliticalBusinessResultBundles.Add(id, bundle);

        var eventLog = ProcessResult(resultId, context);
        AttachBundleData(eventLog, bundle);
        return eventLog;
    }

    public EventLog? ProcessBundleDeleted(Guid id, EventLogBuilderContext context)
    {
        var eventLog = ProcessBundle(id, context);
        context.PoliticalBusinessResultBundles.Remove(id);
        return eventLog;
    }

    public EventLog? ProcessBundle(Guid id, EventLogBuilderContext context)
    {
        if (!context.PoliticalBusinessResultBundles.TryGetValue(id, out var bundle))
        {
            throw new ArgumentException($"Could not initialize an EventLog for bundle {id}, because {nameof(ProcessBundleCreated)} did not get called for the bundle yet");
        }

        var eventLog = ProcessResult(bundle.ResultId, context);
        AttachBundleData(eventLog, bundle);
        return eventLog;
    }

    public EventLog? ProcessBallot(int number, Guid bundleId, EventLogBuilderContext context)
    {
        var eventLog = ProcessBundle(bundleId, context);
        if (eventLog == null)
        {
            return null;
        }

        eventLog.BundleBallotNumber = number;
        return eventLog;
    }

    private void AttachBundleData(EventLog? eventLog, PoliticalBusinessResultBundleAggregate bundle)
    {
        if (eventLog == null)
        {
            return;
        }

        eventLog.BundleNumber = bundle.BundleNumber;
    }
}
