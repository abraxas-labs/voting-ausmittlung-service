﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.EventLogs.EventProcessors;

public abstract class BaseCountingCircleResultReportEventProcessor
{
    public abstract PoliticalBusinessType Type { get; }

    public EventLog? ProcessSubmissionStarted(Guid resultId, Guid politicalBusinessId, Guid countingCircleId, EventLogBuilderContext context)
    {
        context.PoliticalBusinessIdAndCountingCircleIdByResultId.Add(resultId, (politicalBusinessId, countingCircleId));
        return ProcessResult(politicalBusinessId, countingCircleId, context);
    }

    public EventLog? ProcessResult(Guid resultId, EventLogBuilderContext context)
    {
        if (!context.PoliticalBusinessIdAndCountingCircleIdByResultId.TryGetValue(resultId, out var pbIdAndCcId))
        {
            throw new ArgumentException($"Could not initialize an EventLog for result {resultId}, because {nameof(ProcessSubmissionStarted)} did not get called for the result yet");
        }

        return ProcessResult(pbIdAndCcId.PoliticalBusinessId, pbIdAndCcId.CountingCircleId, context);
    }

    public EventLog? ProcessResult(Guid politicalBusinessId, Guid countingCircleId, EventLogBuilderContext context)
    {
        return context.IsPoliticalBusinessIncluded(politicalBusinessId)
            ? new() { PoliticalBusinessId = politicalBusinessId, CountingCircleId = countingCircleId, PoliticalBusinessType = Type }
            : null;
    }
}
