// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class ProportionalElectionEndResultReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<ProportionalElectionListEndResultLotDecisionsUpdated>,
    IReportEventProcessor<ProportionalElectionEndResultFinalized>,
    IReportEventProcessor<ProportionalElectionEndResultFinalizationReverted>,
    IReportEventProcessor<ProportionalElectionManualListEndResultEntered>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.ProportionalElection;

    public EventLog? Process(ProportionalElectionListEndResultLotDecisionsUpdated eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionEndResultFinalized eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionEndResultFinalizationReverted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }

    public EventLog? Process(ProportionalElectionManualListEndResultEntered eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.ProportionalElectionId));
    }
}
