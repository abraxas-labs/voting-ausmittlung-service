// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class ResultOverview
{
    public ResultOverview(
        Contest contest,
        IDictionary<CountingCircle, List<SimpleCountingCircleResult>> countingCircleResults,
        bool currentTenantIsContestManager)
    {
        Contest = contest;
        CountingCircleResults = countingCircleResults;
        CurrentTenantIsContestManager = currentTenantIsContestManager;
    }

    public Contest Contest { get; }

    public IEnumerable<PoliticalBusiness> PoliticalBusinesses => Contest.SimplePoliticalBusinesses;

    public IEnumerable<PoliticalBusinessUnion> PoliticalBusinessUnions => Contest.PoliticalBusinessUnions;

    public IDictionary<CountingCircle, List<SimpleCountingCircleResult>> CountingCircleResults { get; }

    public bool CurrentTenantIsContestManager { get; }
}
