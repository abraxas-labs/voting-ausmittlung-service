// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteResultImportReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<VoteResultImported>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteResultImported eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.VoteId), GuidParser.Parse(eventData.CountingCircleId));
    }
}
