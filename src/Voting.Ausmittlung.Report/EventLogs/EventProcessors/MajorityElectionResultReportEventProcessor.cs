// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionResultReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<MajorityElectionResultSubmissionStarted>,
    IReportEventProcessor<MajorityElectionResultEntryDefined>,
    IReportEventProcessor<MajorityElectionResultCountOfVotersEntered>,
    IReportEventProcessor<MajorityElectionCandidateResultsEntered>,
    IReportEventProcessor<MajorityElectionBallotGroupResultsEntered>,
    IReportEventProcessor<MajorityElectionResultSubmissionFinished>,
    IReportEventProcessor<MajorityElectionResultCorrectionFinished>,
    IReportEventProcessor<MajorityElectionResultFlaggedForCorrection>,
    IReportEventProcessor<MajorityElectionResultAuditedTentatively>,
    IReportEventProcessor<MajorityElectionResultPlausibilised>,
    IReportEventProcessor<MajorityElectionResultResettedToSubmissionFinished>,
    IReportEventProcessor<MajorityElectionResultResettedToAuditedTentatively>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionResultSubmissionStarted eventData, EventLogBuilderContext context)
    {
        return ProcessSubmissionStarted(
            GuidParser.Parse(eventData.ElectionResultId),
            GuidParser.Parse(eventData.ElectionId),
            GuidParser.Parse(eventData.CountingCircleId),
            context);
    }

    public EventLog? Process(MajorityElectionResultEntryDefined eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultCountOfVotersEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionCandidateResultsEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionBallotGroupResultsEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultFlaggedForCorrection eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultAuditedTentatively eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultPlausibilised eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultResettedToSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(MajorityElectionResultResettedToAuditedTentatively eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }
}
