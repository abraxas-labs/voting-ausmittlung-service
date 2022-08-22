// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class SecondaryMajorityElectionResultImportReportEventProcessor :
    BaseCountingCircleResultReportEventProcessor,
    IReportEventProcessor<SecondaryMajorityElectionResultImported>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.SecondaryMajorityElection;

    public EventLog? Process(SecondaryMajorityElectionResultImported eventData, EventLogBuilderContext context)
    {
        return ProcessResult(Guid.Parse(eventData.SecondaryMajorityElectionId), Guid.Parse(eventData.CountingCircleId), context);
    }
}
