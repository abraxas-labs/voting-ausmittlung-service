// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class ResultOverviewCountingCircleResult
{
    public ResultOverviewCountingCircleResult(
        SimpleCountingCircleResult countingCircleResult,
        List<BallotResult> ballotResults,
        List<MajorityElectionCandidateResult> candidateResults,
        int? individualVoteCount,
        List<ProportionalElectionListResult> listResults)
    {
        CountingCircleResult = countingCircleResult;
        BallotResults = ballotResults;
        CandidateResults = candidateResults;
        IndividualVoteCount = individualVoteCount;
        ListResults = listResults;
    }

    public SimpleCountingCircleResult CountingCircleResult { get; }

    public List<BallotResult> BallotResults { get; }

    public List<MajorityElectionCandidateResult> CandidateResults { get; }

    public int? IndividualVoteCount { get; }

    public List<ProportionalElectionListResult> ListResults { get; }
}
