// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

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
        return ProcessBundleCreated(GuidParser.Parse(eventData.BundleId), GuidParser.Parse(eventData.ElectionResultId), eventData.BundleNumber, context);
    }

    public EventLog? Process(MajorityElectionResultBundleDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBundleDeleted(GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleReviewSucceeded eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleReviewRejected eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBundleCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessBundle(GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBallotCreated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBallotUpdated eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, GuidParser.Parse(eventData.BundleId), context);
    }

    public EventLog? Process(MajorityElectionResultBallotDeleted eventData, EventLogBuilderContext context)
    {
        return ProcessBallot(eventData.BallotNumber, GuidParser.Parse(eventData.BundleId), context);
    }
}
