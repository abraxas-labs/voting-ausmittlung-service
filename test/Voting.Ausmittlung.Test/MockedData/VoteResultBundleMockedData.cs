// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData;

public static class VoteResultBundleMockedData
{
    public const string IdGossauBundle1 = "cc757c66-e78c-4f56-907b-6afb27d723d5";
    public const string IdGossauBundle2 = "27d9ee7e-de18-4c95-af40-7d6deee61197";
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
        };

    public static IEnumerable<VoteResultBundle> All
    {
        get
        {
            yield return GossauBundle1;
            yield return GossauBundle2;
            yield return UzwilBundle1;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
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

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var mapper = sp.GetRequiredService<TestMapper>();
            var resultEntryParamsByResultId = db.VoteResults
                .AsEnumerable()
                .ToDictionary(x => x.Id, x => mapper.Map<DomainModels.VoteResultEntryParams>(x.EntryParams));

            var bundles = await db.VoteResultBundles
                .Include(x => x.BallotResult.VoteResult.CountingCircle)
                .Include(x => x.BallotResult.VoteResult.Vote.Contest)
                .ToListAsync();

            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            foreach (var bundle in bundles)
            {
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
                    resultEntryParamsByResultId[bundle.BallotResult.VoteResultId],
                    contestId);
                await aggregateRepository.Save(aggregate);
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
