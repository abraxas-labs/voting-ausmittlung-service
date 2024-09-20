// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData;

public static class PermissionMockedData
{
    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        // temporary set all contest states to testing phase to run permission builder correctly
        var oldStates = new Dictionary<Guid, ContestState>();
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var contests = await db.Contests.AsTracking().Where(x => x.State != ContestState.TestingPhase).ToListAsync();
            oldStates = contests.ToDictionary(x => x.Id, x => x.State);

            foreach (var contest in contests)
            {
                contest.State = ContestState.TestingPhase;
            }

            await db.SaveChangesAsync();
        });

        await runScoped(async sp =>
        {
            var permissionBuilder = sp.GetRequiredService<DomainOfInfluencePermissionBuilder>();
            await permissionBuilder.RebuildPermissionTree();

            foreach (var (id, _) in oldStates.Where(x => x.Value.TestingPhaseEnded()))
            {
                await permissionBuilder.SetContestPermissionsFinal(id);
            }
        });

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var contests = await db.Contests.AsTracking().ToListAsync();

            foreach (var contest in contests)
            {
                if (oldStates.TryGetValue(contest.Id, out var state))
                {
                    contest.State = state;
                }
            }

            await db.SaveChangesAsync();
        });
    }
}
