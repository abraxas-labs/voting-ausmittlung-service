// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData.Mapping;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionResultBallotMockedData
{
    public const string IdUzwilBallot1 = "cd45df54-b093-492f-b3de-37e2c6f5c160";
    public const string IdUzwilBallot2 = "28fc0a51-eb49-4769-bad6-5aed6e6b2782";
    public const string IdUzwilBallotBundle2 = "163fe899-0aea-4210-b7d4-d7248ed99252";

    public static ProportionalElectionResultBallot UzwilBallot1
        => new ProportionalElectionResultBallot
        {
            Id = Guid.Parse(IdUzwilBallot1),
            Number = 1,
            Index = 1,
            EmptyVoteCount = 2,
            BallotCandidates = new List<ProportionalElectionResultBallotCandidate>
            {
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        Position = 1,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId2UzwilProportionalElectionInContestUzwil),
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId11UzwilProportionalElectionInContestUzwil),
                        Position = 3,
                    },
            },
            BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle1),
        };

    public static ProportionalElectionResultBallot UzwilBallot2
        => new ProportionalElectionResultBallot
        {
            Id = Guid.Parse(IdUzwilBallot2),
            Number = 2,
            Index = 3,
            EmptyVoteCount = 4,
            BallotCandidates = new List<ProportionalElectionResultBallotCandidate>
            {
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        Position = 1,
                    },
            },
            BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle1),
        };

    public static ProportionalElectionResultBallot UzwilBallot1Bundle2
        => new ProportionalElectionResultBallot
        {
            Id = Guid.Parse(IdUzwilBallotBundle2),
            Number = 1,
            Index = 1,
            BallotCandidates = new List<ProportionalElectionResultBallotCandidate>
            {
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        Position = 1,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateIdUzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        RemovedFromList = true,
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId12UzwilProportionalElectionInContestUzwil),
                        Position = 2,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId2UzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        RemovedFromList = true,
                        Position = 3,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId12UzwilProportionalElectionInContestUzwil),
                        Position = 3,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId3UzwilProportionalElectionInContestUzwil),
                        OnList = true,
                        Position = 4,
                    },
                    new ProportionalElectionResultBallotCandidate
                    {
                        CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId11UzwilProportionalElectionInContestUzwil),
                        Position = 5,
                    },
            },
            BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle2),
        };

    public static IEnumerable<ProportionalElectionResultBallot> All
    {
        get
        {
            yield return UzwilBallot1;
            yield return UzwilBallot2;
            yield return UzwilBallot1Bundle2;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var all = All.ToList();
            db.ProportionalElectionResultBallots.AddRange(all);

            await db.SaveChangesAsync();

            var ballotsByBundleId = all
                .GroupBy(x => x.BundleId)
                .ToDictionary(x => x.Key);

            var bundles = await db.ProportionalElectionBundles
                .AsTracking()
                .Include(x => x.ElectionResult.ProportionalElection.Contest)
                .Where(x => ballotsByBundleId.Keys.Contains(x.Id))
                .ToListAsync();

            var mapper = sp.GetRequiredService<TestMapper>();

            foreach (var bundle in bundles)
            {
                var ballots = ballotsByBundleId[bundle.Id];
                bundle.CountOfBallots = ballots.Count();

                await runScoped(async newSp =>
                {
                    // needed to create aggregates, since they access user/tenant information
                    var authStore = newSp.GetRequiredService<IAuthStore>();
                    authStore.SetValues("mock-token", bundle.CreatedBy.SecureConnectId, "test", Enumerable.Empty<string>());

                    var aggregateRepository = newSp.GetRequiredService<IAggregateRepository>();
                    var contestId = bundle.ElectionResult.ProportionalElection.ContestId;

                    var aggregate = await aggregateRepository.GetOrCreateById<ProportionalElectionResultBundleAggregate>(bundle.Id);
                    foreach (var ballot in ballots)
                    {
                        aggregate.CreateBallot(
                            null,
                            ballot.EmptyVoteCount,
                            ballot.BallotCandidates.Select(x => mapper.Map<Core.Domain.ProportionalElectionResultBallotCandidate>(x)).ToList().AsReadOnly(),
                            contestId);
                    }

                    await aggregateRepository.Save(aggregate);
                });
            }
        });
    }
}
