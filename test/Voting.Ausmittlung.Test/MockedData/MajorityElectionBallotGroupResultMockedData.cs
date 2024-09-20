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

public static class MajorityElectionBallotGroupResultMockedData
{
    public static MajorityElectionBallotGroupResult StGallenBallotGroupResult1
        => new MajorityElectionBallotGroupResult
        {
            Id = Guid.NewGuid(),
            BallotGroupId = Guid.Parse(MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund),
            ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
            VoteCount = 2,
        };

    public static IEnumerable<MajorityElectionBallotGroupResult> All
    {
        get { yield return StGallenBallotGroupResult1; }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var electionResults = await db.MajorityElectionResults
                .AsTracking()
                .Where(x => All.Select(y => y.ElectionResultId).Contains(x.Id))
                .Include(x => x.BallotGroupResults)
                .ToListAsync();

            foreach (var ballotGroupResult in All)
            {
                electionResults.First(x => x.Id == ballotGroupResult.ElectionResultId)
                    .BallotGroupResults.Clear();
            }

            db.MajorityElectionBallotGroupResults.AddRange(All);

            await db.SaveChangesAsync();
        });
    }
}
