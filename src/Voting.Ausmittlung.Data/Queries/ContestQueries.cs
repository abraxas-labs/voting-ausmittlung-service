// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Queries;

public static class ContestQueries
{
    public static IQueryable<T> WhereContestIsInTestingPhase<T>(this IQueryable<T> query)
        where T : class, IHasSnapshotContest
        => query.Where(x => x.SnapshotContest != null && x.SnapshotContest.State <= ContestState.TestingPhase);

    public static IQueryable<Contest> WhereInTestingPhase(this IQueryable<Contest> query)
        => query.Where(x => x.State <= ContestState.TestingPhase);

    public static IQueryable<Contest> WhereTestingPhaseEnded(this IQueryable<Contest> query)
        => query.Where(x => x.State > ContestState.TestingPhase);

    public static IQueryable<Contest> WhereIsContestManagerAndInTestingPhase(this IQueryable<Contest> query, string tenantId)
        => query.WhereInTestingPhase().Where(x => x.DomainOfInfluence.SecureConnectId == tenantId);

    public static IQueryable<T> WhereContestIsInTestingPhaseOrNoContest<T>(this IQueryable<T> query)
        where T : class, IHasSnapshotContest
        => query.Where(x => x.SnapshotContest == null || x.SnapshotContest.State <= ContestState.TestingPhase);

    public static IQueryable<T> WhereContestIsNotInTestingPhase<T>(this IQueryable<T> query)
        where T : class, IHasSnapshotContest
        => query.Where(x => x.SnapshotContest != null && x.SnapshotContest.State > ContestState.TestingPhase);
}
