// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteResultBundleReportEventProcessor :
    BasePoliticalBusinessResultBundleReportEventProcessor,
    IReportEventProcessor<VoteResultBundleCreated>,
    IReportEventProcessor<VoteResultBundleDeleted>,
    IReportEventProcessor<VoteResultBundleReviewSucceeded>,
    IReportEventProcessor<VoteResultBundleReviewRejected>,
    IReportEventProcessor<VoteResultBundleSubmissionFinished>,
    IReportEventProcessor<VoteResultBundleCorrectionFinished>,
    IReportEventProcessor<VoteResultBallotCreated>,
    IReportEventProcessor<VoteResultBallotUpdated>,
    IReportEventProcessor<VoteResultBallotDeleted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteResultBundleCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBundleCreated(Guid.Parse(eventData.BundleId), Guid.Parse(eventData.VoteResultId), eventData.BundleNumber, context);
    }

    public EventLog? Process(VoteResultBundleDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBundleDeleted(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBundleReviewSucceeded eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBundleReviewRejected eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBundleSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBundleCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBallotCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBallotUpdated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(VoteResultBallotDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }
}
