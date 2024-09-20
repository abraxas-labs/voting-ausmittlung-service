// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteResultReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<VoteResultSubmissionStarted>,
    IReportEventProcessor<VoteResultEntryDefined>,
    IReportEventProcessor<VoteResultEntered>,
    IReportEventProcessor<VoteResultCorrectionEntered>,
    IReportEventProcessor<VoteResultSubmissionFinished>,
    IReportEventProcessor<VoteResultCorrectionFinished>,
    IReportEventProcessor<VoteResultFlaggedForCorrection>,
    IReportEventProcessor<VoteResultAuditedTentatively>,
    IReportEventProcessor<VoteResultPlausibilised>,
    IReportEventProcessor<VoteResultResettedToSubmissionFinished>,
    IReportEventProcessor<VoteResultResettedToAuditedTentatively>,
    IReportEventProcessor<VoteResultCountOfVotersEntered>,
    IReportEventProcessor<VoteResultResetted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteResultSubmissionStarted eventData, EventLogBuilderContext context)
    {
        return ProcessSubmissionStarted(
            GuidParser.Parse(eventData.VoteResultId),
            GuidParser.Parse(eventData.VoteId),
            GuidParser.Parse(eventData.CountingCircleId),
            context);
    }

    public EventLog? Process(VoteResultEntryDefined eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultCorrectionEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultCorrectionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultFlaggedForCorrection eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultAuditedTentatively eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultPlausibilised eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultResettedToSubmissionFinished eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultResettedToAuditedTentatively eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultCountOfVotersEntered eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }

    public EventLog? Process(VoteResultResetted eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteResultId), context);
    }
}
