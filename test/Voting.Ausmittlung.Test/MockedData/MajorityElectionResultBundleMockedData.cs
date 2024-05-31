// (c) Copyright 2024 by Abraxas Informatik AG
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
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData;

public static class MajorityElectionResultBundleMockedData
{
    public const string IdStGallenBundle1 = "6c73b736-cc42-4325-a8be-7e5233f3d54e";
    public const string IdStGallenBundle2 = "671ababa-3802-4687-a279-843ec8ca4cf9";
    public const string IdStGallenBundle3 = "217ca0a0-72f6-4583-911e-46337a6f102e";
    public const string IdKircheBundle1 = "901c8592-ec60-45f0-9795-3e792dcf68ae";

    public static MajorityElectionResultBundle StGallenBundle1
        => new MajorityElectionResultBundle
        {
            Id = Guid.Parse(IdStGallenBundle1),
            Number = 1,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
        };

    public static MajorityElectionResultBundle StGallenBundle2
        => new MajorityElectionResultBundle
        {
            Id = Guid.Parse(IdStGallenBundle2),
            Number = 2,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
        };

    public static MajorityElectionResultBundle StGallenBundle3
        => new MajorityElectionResultBundle
        {
            Id = Guid.Parse(IdStGallenBundle3),
            Number = 3,
            CreatedBy =
            {
                FirstName = "Someone",
                LastName = "Else",
                SecureConnectId = "someones-user-id",
            },
            ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
        };

    public static MajorityElectionResultBundle KircheBundle1
        => new MajorityElectionResultBundle
        {
            Id = Guid.Parse(IdKircheBundle1),
            Number = 1,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = Guid.Parse(MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche),
        };

    public static IEnumerable<MajorityElectionResultBundle> All
    {
        get
        {
            yield return StGallenBundle1;
            yield return StGallenBundle2;
            yield return StGallenBundle3;
            yield return KircheBundle1;
        }
    }

    public static Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        return runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var electionResults = await db.MajorityElectionResults
                .AsTracking()
                .Where(x => All.Select(y => y.ElectionResultId).Contains(x.Id))
                .ToListAsync();
            db.MajorityElectionResultBundles.AddRange(All);

            foreach (var bundle in All)
            {
                electionResults.First(x => x.Id == bundle.ElectionResultId)
                    .CountOfBundlesNotReviewedOrDeleted++;
            }

            await db.SaveChangesAsync();

            var bundles = await db.MajorityElectionResultBundles
                .Include(x => x.ElectionResult.CountingCircle)
                .Include(x => x.ElectionResult.MajorityElection.Contest)
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
                    var contestId = bundle.ElectionResult.MajorityElection.ContestId;
                    var resultAggregate = await GetOrCreateElectionResultAggregate(aggregateFactory, aggregateRepository, bundle);
                    resultAggregate.BundleNumberEntered(bundle.Number, contestId);
                    await aggregateRepository.Save(resultAggregate);

                    var aggregate = aggregateFactory.New<MajorityElectionResultBundleAggregate>();
                    aggregate.Create(
                        bundle.Id,
                        bundle.ElectionResultId,
                        bundle.Number,
                        MajorityElectionResultEntry.Detailed,
                        mapper.Map<DomainModels.MajorityElectionResultEntryParams>(bundle.ElectionResult.EntryParams),
                        contestId);
                    await aggregateRepository.Save(aggregate);
                });
            }
        });
    }

    private static async Task<MajorityElectionResultAggregate> GetOrCreateElectionResultAggregate(
        IAggregateFactory factory,
        IAggregateRepository repository,
        MajorityElectionResultBundle bundle)
    {
        try
        {
            return await repository.GetById<MajorityElectionResultAggregate>(bundle.ElectionResultId);
        }
        catch (AggregateNotFoundException)
        {
            var resultAggregate = factory.New<MajorityElectionResultAggregate>();
            resultAggregate.StartSubmission(
                bundle.ElectionResult.CountingCircle.BasisCountingCircleId,
                bundle.ElectionResult.MajorityElectionId,
                bundle.ElectionResult.MajorityElection.ContestId,
                bundle.ElectionResult.MajorityElection.Contest.TestingPhaseEnded);
            return resultAggregate;
        }
    }
}
