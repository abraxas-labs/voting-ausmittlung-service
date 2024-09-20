// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Extensions;

public static class CountingCircleResultExtensions
{
    public static DateTime? GetLastStateChangeTimestamp(this CountingCircleResult result)
    {
        return result.PlausibilisedTimestamp
            ?? result.AuditedTentativelyTimestamp
            ?? result.SubmissionDoneTimestamp
            ?? result.ReadyForCorrectionTimestamp;
    }
}
