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
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Exceptions;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData;

public static class VoteResultBundleMockedData
{
    public const string IdGossauBundle1 = "cc757c66-e78c-4f56-907b-6afb27d723d5";
    public const string IdGossauBundle2 = "27d9ee7e-de18-4c95-af40-7d6deee61197";
    public const string IdGossauBundle3 = "a2249597-4a74-4d27-a66b-e5159eb470a4";
    public const string IdUzwilBundle1 = "46b6e875-6e63-4a68-a7ef-96e0f667c40b";

    public static VoteResultBundle GossauBundle1
        => new VoteResultBundle
        {
            Id = Guid.Parse(IdGossauBundle1),
            Number = 1,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            BallotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult),
            Logs = new()
            {
                new()
                {
                    User =
                    {
                        FirstName = "Hans",
                        LastName = "Muster",
                        SecureConnectId = TestDefaults.UserId,
                    },
                    Timestamp = MockedClock.UtcNowDate,
                    State = BallotBundleState.InProcess,
                },
            },
        };

    public static VoteResultBundle GossauBundle2
        => new VoteResultBundle
        {
            Id = Guid.Parse(IdGossauBundle2),
            Number = 2,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            BallotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult),
            Logs = new()
            {
                new()
                {
                    User =
                    {
                        FirstName = "Hans",
                        LastName = "Muster",
                        SecureConnectId = TestDefaults.UserId,
                    },
                    Timestamp = MockedClock.UtcNowDate,
                    State = BallotBundleState.InProcess,
                },
            },
        };

    public static VoteResultBundle GossauBundle3
        => new VoteResultBundle
        {
            Id = Guid.Parse(IdGossauBundle3),
            Number = 3,
            CreatedBy =
            {
                    FirstName = "Someone",
                    LastName = "Else",
                    SecureConnectId = "someones-user-id",
            },
            BallotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult),
            Logs = new()
            {
                new()
                {
                    User =
                    {
                        FirstName = "Someone",
                        LastName = "Else",
                        SecureConnectId = "someones-user-id",
                    },
                    Timestamp = MockedClock.UtcNowDate,
                    State = BallotBundleState.InProcess,
                },
            },
        };

    public static VoteResultBundle UzwilBundle1
        => new VoteResultBundle
        {
            Id = Guid.Parse(IdUzwilBundle1),
            Number = 1,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            BallotResultId = Guid.Parse(VoteResultMockedData.IdUzwilVoteInContestStGallenBallotResult),
            Logs = new()
            {
                new()
                {
                    User =
                    {
                        FirstName = "Hans",
                        LastName = "Muster",
                        SecureConnectId = TestDefaults.UserId,
                    },
                    Timestamp = MockedClock.UtcNowDate,
                    State = BallotBundleState.InProcess,
                },
            },
        };

    public static IEnumerable<VoteResultBundle> All
    {
        get
        {
            yield return GossauBundle1;
            yield return GossauBundle2;
            yield return GossauBundle3;
            yield return UzwilBundle1;
        }
    }

    public static Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        return runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var ballotResults = await db.BallotResults
                .AsTracking()
                .Where(x => All.Select(y => y.BallotResultId).Contains(x.Id))
                .ToListAsync();
            db.VoteResultBundles.AddRange(All);

            foreach (var bundle in All)
            {
                ballotResults.First(x => x.Id == bundle.BallotResultId)
                    .CountOfBundlesNotReviewedOrDeleted++;
            }

            await db.SaveChangesAsync();

            var bundles = await db.VoteResultBundles
                .Include(x => x.BallotResult.VoteResult.CountingCircle)
                .Include(x => x.BallotResult.VoteResult.Vote.Contest)
                .ToListAsync();

            var mapper = sp.GetRequiredService<TestMapper>();

            foreach (var bundle in bundles)
            {
                await runScoped(async newSp =>
                {
                    // needed to create aggregates, since they access user/tenant information
                    var authStore = newSp.GetRequiredService<IAuthStore>();
                    authStore.SetValues("mock-token", bundle.CreatedBy.SecureConnectId, "test", Enumerable.Empty<string>());

                    var aggregateFactory = newSp.GetRequiredService<IAggregateFactory>();
                    var aggregateRepository = newSp.GetRequiredService<IAggregateRepository>();
                    var contestId = bundle.BallotResult.VoteResult.Vote.ContestId;
                    var resultAggregate = await GetOrCreateVoteResultAggregate(aggregateFactory, aggregateRepository, bundle);
                    resultAggregate.BundleNumberEntered(bundle.Number, bundle.BallotResultId, contestId);
                    await aggregateRepository.Save(resultAggregate);

                    var aggregate = aggregateFactory.New<VoteResultBundleAggregate>();
                    aggregate.Create(
                        bundle.Id,
                        bundle.BallotResult.VoteResultId,
                        bundle.BallotResultId,
                        bundle.Number,
                        mapper.Map<DomainModels.VoteResultEntryParams>(bundle.BallotResult.VoteResult.EntryParams),
                        contestId);
                    await aggregateRepository.Save(aggregate);
                });
            }
        });
    }

    private static async Task<VoteResultAggregate> GetOrCreateVoteResultAggregate(
        IAggregateFactory factory,
        IAggregateRepository repository,
        VoteResultBundle bundle)
    {
        try
        {
            return await repository.GetById<VoteResultAggregate>(bundle.BallotResult.VoteResultId);
        }
        catch (AggregateNotFoundException)
        {
            var resultAggregate = factory.New<VoteResultAggregate>();
            resultAggregate.StartSubmission(
                bundle.BallotResult.VoteResult.CountingCircle.BasisCountingCircleId,
                bundle.BallotResult.VoteResult.VoteId,
                bundle.BallotResult.VoteResult.Vote.ContestId,
                bundle.BallotResult.VoteResult.Vote.Contest.TestingPhaseEnded);
            return resultAggregate;
        }
    }
}
