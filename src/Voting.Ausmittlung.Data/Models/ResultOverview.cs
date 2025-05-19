// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ResultOverview
{
    public ResultOverview(
        Contest contest,
        IDictionary<CountingCircle, List<ResultOverviewCountingCircleResult>> countingCircleResults,
        bool currentTenantIsContestManager,
        bool hasPartialResults)
    {
        Contest = contest;
        CountingCircleResults = countingCircleResults;
        CurrentTenantIsContestManager = currentTenantIsContestManager;
        HasPartialResults = hasPartialResults;
    }

    public Contest Contest { get; }

    public IEnumerable<PoliticalBusiness> PoliticalBusinesses => Contest.SimplePoliticalBusinesses;

    public IEnumerable<PoliticalBusinessUnion> PoliticalBusinessUnions => Contest.PoliticalBusinessUnions;

    public IDictionary<CountingCircle, List<ResultOverviewCountingCircleResult>> CountingCircleResults { get; }

    public bool CurrentTenantIsContestManager { get; }

    public bool HasPartialResults { get; }
}
