// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionEndResultReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<ProportionalElectionListEndResultLotDecisionsUpdated>,
    IReportEventProcessor<ProportionalElectionEndResultFinalized>,
    IReportEventProcessor<ProportionalElectionEndResultFinalizationReverted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionListEndResultLotDecisionsUpdated eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.ProportionalElectionId), context);
    }

    public EventLog? Process(ProportionalElectionEndResultFinalized eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.ProportionalElectionId), context);
    }

    public EventLog? Process(ProportionalElectionEndResultFinalizationReverted eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.ProportionalElectionId), context);
    }
}
