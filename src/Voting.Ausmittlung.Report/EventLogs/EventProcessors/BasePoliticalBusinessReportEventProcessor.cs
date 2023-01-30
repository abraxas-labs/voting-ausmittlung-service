// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public abstract class BasePoliticalBusinessReportEventProcessor
{
    public abstract PoliticalBusinessType Type { get; }

    protected EventLog? Process(Guid politicalBusinessId)
    {
        return new() { PoliticalBusinessId = politicalBusinessId, PoliticalBusinessType = Type };
    }
}
