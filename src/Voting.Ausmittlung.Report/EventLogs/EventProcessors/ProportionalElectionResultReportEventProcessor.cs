// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionResultReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<ProportionalElectionResultSubmissionStarted>,
    IReportEventProcessor<ProportionalElectionResultEntryDefined>,
    IReportEventProcessor<ProportionalElectionResultCountOfVotersEntered>,
    IReportEventProcessor<ProportionalElectionUnmodifiedListResultsEntered>,
    IReportEventProcessor<ProportionalElectionResultSubmissionFinished>,
    IReportEventProcessor<ProportionalElectionResultCorrectionFinished>,
    IReportEventProcessor<ProportionalElectionResultFlaggedForCorrection>,
    IReportEventProcessor<ProportionalElectionResultAuditedTentatively>,
    IReportEventProcessor<ProportionalElectionResultPlausibilised>,
    IReportEventProcessor<ProportionalElectionResultResettedToSubmissionFinished>,
    IReportEventProcessor<ProportionalElectionResultResettedToAuditedTentatively>,
    IReportEventProcessor<ProportionalElectionResultResetted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionResultSubmissionStarted eventData, EventLogBuilderContext context)
    {
        return ProcessSubmissionStarted(
            GuidParser.Parse(eventData.ElectionResultId),
            GuidParser.Parse(eventData.ElectionId),
            GuidParser.Parse(eventData.CountingCircleId),
            context);
    }

    public EventLog? Process(ProportionalElectionResultEntryDefined eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultCountOfVotersEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionUnmodifiedListResultsEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultFlaggedForCorrection eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultAuditedTentatively eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultPlausibilised eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultResettedToSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultResettedToAuditedTentatively eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }

    public EventLog? Process(ProportionalElectionResultResetted eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ElectionResultId), context);
    }
}
