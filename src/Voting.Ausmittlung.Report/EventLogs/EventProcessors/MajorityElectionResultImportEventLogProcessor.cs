// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionResultImportEventLogProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<MajorityElectionResultImported>,
    IReportEventProcessor<MajorityElectionWriteInsReset>,
    IReportEventProcessor<MajorityElectionWriteInsMapped>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionResultImported eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.MajorityElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }

    public EventLog? Process(MajorityElectionWriteInsMapped eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.MajorityElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }

    public EventLog? Process(MajorityElectionWriteInsReset eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.MajorityElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }
}
