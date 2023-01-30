// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

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
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionEndResultFinalized eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }

    public EventLog? Process(MajorityElectionEndResultFinalizationReverted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.MajorityElectionId));
    }
}
