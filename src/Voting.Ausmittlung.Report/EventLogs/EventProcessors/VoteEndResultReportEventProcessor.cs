// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteEndResultReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<VoteEndResultFinalized>,
    IReportEventProcessor<VoteEndResultFinalizationReverted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteEndResultFinalized eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.VoteId));
    }

    public EventLog? Process(VoteEndResultFinalizationReverted eventData, EventLogBuilderContext context)
    {
        return Process(GuidParser.Parse(eventData.VoteId));
    }
}
