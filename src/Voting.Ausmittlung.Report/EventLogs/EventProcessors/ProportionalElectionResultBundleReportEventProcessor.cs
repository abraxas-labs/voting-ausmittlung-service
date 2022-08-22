// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionResultBundleReportEventProcessor :
    BasePoliticalBusinessResultBundleReportEventProcessor,
    IReportEventProcessor<ProportionalElectionResultBundleCreated>,
    IReportEventProcessor<ProportionalElectionResultBundleDeleted>,
    IReportEventProcessor<ProportionalElectionResultBundleReviewSucceeded>,
    IReportEventProcessor<ProportionalElectionResultBundleReviewRejected>,
    IReportEventProcessor<ProportionalElectionResultBallotCreated>,
    IReportEventProcessor<ProportionalElectionResultBallotUpdated>,
    IReportEventProcessor<ProportionalElectionResultBallotDeleted>,
    IReportEventProcessor<ProportionalElectionResultBundleSubmissionFinished>,
    IReportEventProcessor<ProportionalElectionResultBundleCorrectionFinished>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionResultBundleCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBundleCreated(Guid.Parse(eventData.BundleId), Guid.Parse(eventData.ElectionResultId), eventData.BundleNumber, context);
    }

    public EventLog? Process(ProportionalElectionResultBundleDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBundleDeleted(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBundleReviewSucceeded eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBundleReviewRejected eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBundleSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBundleCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBallotCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBallotUpdated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(ProportionalElectionResultBallotDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }
}
