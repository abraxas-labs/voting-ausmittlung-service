// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionResultImportReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<ProportionalElectionResultImported>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionResultImported eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.ProportionalElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }
}
