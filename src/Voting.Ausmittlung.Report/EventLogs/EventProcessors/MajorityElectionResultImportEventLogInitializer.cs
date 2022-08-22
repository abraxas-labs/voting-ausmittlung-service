// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionResultImportEventLogInitializer :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<MajorityElectionWriteInsMapped>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionWriteInsMapped eventData, EventLogBuilderContext context)
    {
        return ProcessResult(Guid.Parse(eventData.MajorityElectionId), Guid.Parse(eventData.CountingCircleId), context);
    }
}
