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

public static class ProportionalElectionResultBundleMockedData
{
    public const string IdGossauBundle1 = "e0cf709d-f8eb-477a-b57d-ce058c3ce186";
    public const string IdGossauBundle2 = "e6be0892-eb02-41a7-a32b-d5b71de926f3";
    public const string IdGossauBundle3 = "ce73c633-8689-4e8d-bb77-612f354b4ad6";
    public const string IdUzwilBundle1 = "15422d9e-8c4a-48cf-92b4-8c37c373a03e";
    public const string IdUzwilBundle2 = "8dfe615f-b277-4003-9083-113fb2742b64";
    public const string IdUzwilBundle3 = "9b2ae103-37b3-4aab-88af-b1da670c0e6e";

    public static ProportionalElectionResultBundle GossauBundle1List1
        => new ProportionalElectionResultBundle
        {
            Id = Guid.Parse(IdGossauBundle1),
            ListId = Guid.Parse(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen),
            Number = 1,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen,
        };

    public static ProportionalElectionResultBundle GossauBundle2NoList
        => new ProportionalElectionResultBundle
        {
            Id = Guid.Parse(IdGossauBundle2),
            Number = 2,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen,
        };

    public static ProportionalElectionResultBundle GossauBundle3
        => new ProportionalElectionResultBundle
        {
            Id = Guid.Parse(IdGossauBundle3),
            ListId = Guid.Parse(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen),
            Number = 3,
            CreatedBy =
            {
                FirstName = "Someone",
                LastName = "Else",
                SecureConnectId = "someones-user-id",
            },
            ElectionResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen,
        };

    public static ProportionalElectionResultBundle UzwilBundle1NoList
        => new ProportionalElectionResultBundle
        {
            Id = Guid.Parse(IdUzwilBundle1),
            Number = 1,
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
        };

    public static ProportionalElectionResultBundle UzwilBundle2
        => new ProportionalElectionResultBundle
        {
            Id = Guid.Parse(IdUzwilBundle2),
            Number = 2,
            ListId = Guid.Parse(ProportionalElectionMockedData.ListIdUzwilProportionalElectionInContestUzwil),
            CreatedBy =
            {
                    FirstName = "Hans",
                    LastName = "Muster",
                    SecureConnectId = TestDefaults.UserId,
            },
            ElectionResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
        };

    public static ProportionalElectionResultBundle UzwilBundle3
        => new ProportionalElectionResultBundle
        {
            Id = Guid.Parse(IdUzwilBundle3),
            Number = 3,
            CreatedBy =
            {
                FirstName = "Someone",
                LastName = "Else",
                SecureConnectId = "someones-user-id",
            },
            ElectionResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
        };

    public static IEnumerable<ProportionalElectionResultBundle> All
    {
        get
        {
            yield return GossauBundle1List1;
            yield return GossauBundle2NoList;
            yield return GossauBundle3;
            yield return UzwilBundle1NoList;
            yield return UzwilBundle2;
            yield return UzwilBundle3;
        }
    }

    public static Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        return runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var electionResults = await db.ProportionalElectionResults
                .AsTracking()
                .Where(x => All.Select(y => y.ElectionResultId).Contains(x.Id))
                .ToListAsync();
            db.ProportionalElectionBundles.AddRange(All);

            foreach (var bundle in All)
            {
                electionResults.First(x => x.Id == bundle.ElectionResultId)
                    .CountOfBundlesNotReviewedOrDeleted++;
            }

            await db.SaveChangesAsync();

            var bundles = await db.ProportionalElectionBundles
                .Include(x => x.ElectionResult.CountingCircle)
                .Include(x => x.ElectionResult.ProportionalElection.Contest)
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
                    var contestId = bundle.ElectionResult.ProportionalElection.ContestId;
                    var resultAggregate = await GetOrCreateElectionResultAggregate(aggregateFactory, aggregateRepository, bundle);
                    resultAggregate.BundleNumberEntered(bundle.Number, contestId);
                    await aggregateRepository.Save(resultAggregate);

                    var aggregate = aggregateFactory.New<ProportionalElectionResultBundleAggregate>();
                    aggregate.Create(
                        bundle.Id,
                        bundle.ElectionResultId,
                        bundle.ListId,
                        bundle.Number,
                        mapper.Map<DomainModels.ProportionalElectionResultEntryParams>(bundle.ElectionResult.EntryParams),
                        contestId);
                    await aggregateRepository.Save(aggregate);
                });
            }
        });
    }

    private static async Task<ProportionalElectionResultAggregate> GetOrCreateElectionResultAggregate(
        IAggregateFactory factory,
        IAggregateRepository repository,
        ProportionalElectionResultBundle bundle)
    {
        try
        {
            return await repository.GetById<ProportionalElectionResultAggregate>(bundle.ElectionResultId);
        }
        catch (AggregateNotFoundException)
        {
            var resultAggregate = factory.New<ProportionalElectionResultAggregate>();
            resultAggregate.StartSubmission(
                bundle.ElectionResult.CountingCircle.BasisCountingCircleId,
                bundle.ElectionResult.ProportionalElectionId,
                bundle.ElectionResult.ProportionalElection.ContestId,
                bundle.ElectionResult.ProportionalElection.Contest.TestingPhaseEnded);
            return resultAggregate;
        }
    }
}
