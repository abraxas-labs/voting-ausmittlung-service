// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ResultOverviewCountingCircleResult
{
    public ResultOverviewCountingCircleResult(
        SimpleCountingCircleResult countingCircleResult,
        List<BallotResult> ballotResults)
    {
        CountingCircleResult = countingCircleResult;
        BallotResults = ballotResults;
    }

    public SimpleCountingCircleResult CountingCircleResult { get; }

    public List<BallotResult> BallotResults { get; }
}
