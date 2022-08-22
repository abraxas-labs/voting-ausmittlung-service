// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public class VoteEndResultReportEventProcessor :
    BasePoliticalBusinessReportEventProcessor,
    IReportEventProcessor<VoteEndResultFinalized>,
    IReportEventProcessor<VoteEndResultFinalizationReverted>
{
    public override PoliticalBusinessType Type => PoliticalBusinessType.Vote;

    public EventLog? Process(VoteEndResultFinalized eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.VoteId), context);
    }

    public EventLog? Process(VoteEndResultFinalizationReverted eventData, EventLogBuilderContext context)
    {
        return Process(Guid.Parse(eventData.VoteId), context);
    }
}
