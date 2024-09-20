// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class SecondaryMajorityElectionResultImportReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<SecondaryMajorityElectionResultImported>,
    IReportEventProcessor<SecondaryMajorityElectionWriteInsMapped>,
    IReportEventProcessor<SecondaryMajorityElectionWriteInsReset>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.SecondaryMajorityElection;

    public EventLog? Process(SecondaryMajorityElectionResultImported eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.SecondaryMajorityElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }

    public EventLog? Process(SecondaryMajorityElectionWriteInsMapped eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.SecondaryMajorityElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }

    public EventLog? Process(SecondaryMajorityElectionWriteInsReset eventData, EventLogBuilderContext context)
    {
        return ProcessResult(GuidParser.Parse(eventData.SecondaryMajorityElectionId), GuidParser.Parse(eventData.CountingCircleId));
    }
}
