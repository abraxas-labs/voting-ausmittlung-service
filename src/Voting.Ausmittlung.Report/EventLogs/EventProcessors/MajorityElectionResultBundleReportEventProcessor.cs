// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionResultBundleReportEventProcessor :
    BasePoliticalBusinessResultBundleReportEventProcessor,
    IReportEventProcessor<MajorityElectionResultBundleCreated>,
    IReportEventProcessor<MajorityElectionResultBundleDeleted>,
    IReportEventProcessor<MajorityElectionResultBundleReviewSucceeded>,
    IReportEventProcessor<MajorityElectionResultBundleReviewRejected>,
    IReportEventProcessor<MajorityElectionResultBundleSubmissionFinished>,
    IReportEventProcessor<MajorityElectionResultBundleCorrectionFinished>,
    IReportEventProcessor<MajorityElectionResultBallotCreated>,
    IReportEventProcessor<MajorityElectionResultBallotUpdated>,
    IReportEventProcessor<MajorityElectionResultBallotDeleted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionResultBundleCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBundleCreated(Guid.Parse(eventData.BundleId), Guid.Parse(eventData.ElectionResultId), eventData.BundleNumber, context);
    }

    public EventLog? Process(MajorityElectionResultBundleDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBundleDeleted(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleReviewSucceeded eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleReviewRejected eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBallotCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBallotUpdated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBallotDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, Guid.Parse(eventData.BundleId), context);
    }
}
