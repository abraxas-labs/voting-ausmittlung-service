// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionUnmodifiedListResultMockedData
{
    public static ProportionalElectionUnmodifiedListResult UzwilUnmodifiedListResult1
        => new ProportionalElectionUnmodifiedListResult
        {
            Id = Guid.Parse("9b69833f-12e0-425d-8102-dc6232c673be"),
            ListId = Guid.Parse(ProportionalElectionMockedData.ListIdUzwilProportionalElectionInContestUzwil),
            ResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
            ConventionalVoteCount = 2,
        };

    public static IEnumerable<ProportionalElectionUnmodifiedListResult> All
    {
        get { yield return UzwilUnmodifiedListResult1; }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var all = All.ToList();
            var resultIds = all.Select(x => x.ResultId).ToHashSet();
            var listIds = all.Select(x => x.ListId).ToHashSet();
            var listResults = await db.ProportionalElectionUnmodifiedListResults
                .Where(x => resultIds.Contains(x.ResultId) && listIds.Contains(x.ListId))
                .ToListAsync();

            var listResultsByResultAndListId = listResults.ToDictionary(x => (x.ResultId, x.ListId));
            var toRemove = all.Select(x => listResultsByResultAndListId[(x.ResultId, x.ListId)]);
            db.ProportionalElectionUnmodifiedListResults.RemoveRange(toRemove);
            db.ProportionalElectionUnmodifiedListResults.AddRange(all);

            await db.SaveChangesAsync();
        });
    }
}
