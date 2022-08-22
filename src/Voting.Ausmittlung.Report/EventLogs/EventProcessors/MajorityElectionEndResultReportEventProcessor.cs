// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class MajorityElectionEndResultReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<MajorityElectionEndResultLotDecisionsUpdated>,
    IReportEventProcessor<MajorityElectionEndResultFinalized>,
    IReportEventProcessor<MajorityElectionEndResultFinalizationReverted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.MajorityElection;

    public EventLog? Process(MajorityElectionEndResultLotDecisionsUpdated eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.MajorityElectionId), context);
    }

    public EventLog? Process(MajorityElectionEndResultFinalized eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.MajorityElectionId), context);
    }

    public EventLog? Process(MajorityElectionEndResultFinalizationReverted eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.MajorityElectionId), context);
    }
}
