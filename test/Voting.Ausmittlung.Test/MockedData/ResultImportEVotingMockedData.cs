// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using ResultImport = Voting.Ausmittlung.Data.Models.ResultImport;
using User = Voting.Ausmittlung.Data.Models.User;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ResultImportEVotingMockedData
{
    public static ResultImport Uzwil1 => new ResultImport
    {
        Completed = true,
        Id = Guid.Parse("d02ad8dc-2372-4f4e-afb7-9b55c6a512ae"),
        Started = MockedClock.GetDate(-10),
        ContestId = Guid.Parse(ContestMockedData.IdUzwilEVoting),
        FileName = "import_uzwil_1.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "c94eb4da-cf1a-4fb1-bad3-113fd9750579",
        },
        EmptyCountingCircles = new List<EmptyImportCountingCircle>
        {
            new EmptyImportCountingCircle
            {
                CountingCircleId = CountingCircleMockedData.StGallen.Id,
            },
        },
        ImportedCountingCircles = new List<ResultImportCountingCircle>
        {
            new ResultImportCountingCircle
            {
                CountingCircleId = CountingCircleMockedData.Uzwil.Id,
            },
        },
        ImportType = ResultImportType.EVoting,
    };

    public static ResultImport UzwilDeleted => new ResultImport
    {
        Id = Guid.Parse("57fdd782-3185-435d-8d84-95bc9f0b378d"),
        Started = MockedClock.GetDate(-5),
        Deleted = true,
        ContestId = Guid.Parse(ContestMockedData.IdUzwilEVoting),
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "c94eb4da-cf1a-4fb1-bad3-113fd9750579",
        },
        ImportType = ResultImportType.EVoting,
    };

    public static ResultImport Uzwil2 => new ResultImport
    {
        Id = Guid.Parse("e45540a9-d412-4cc2-a219-12b052a2b58e"),
        Started = MockedClock.GetDate(-2),
        ContestId = Guid.Parse(ContestMockedData.IdUzwilEVoting),
        FileName = "import_uzwil_2.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "c94eb4da-cf1a-4fb1-bad3-113fd9750579",
        },
        EmptyCountingCircles = new List<EmptyImportCountingCircle>
        {
            new EmptyImportCountingCircle
            {
                CountingCircleId = CountingCircleMockedData.Gossau.Id,
            },
        },
        IgnoredCountingCircles = new List<IgnoredImportCountingCircle>
        {
            new()
            {
                CountingCircleId = "13297",
                CountingCircleDescription = "SU_2_Vilters",
                IsTestCountingCircle = true,
            },
            new()
            {
                CountingCircleId = "unknown",
            },
        },
        ImportType = ResultImportType.EVoting,
    };

    public static ResultImport Gossau1 => new ResultImport
    {
        Id = Guid.Parse("17718f28-22bc-4dc4-b979-e3f4bf22c574"),
        Started = MockedClock.GetDate(-5),
        ContestId = Guid.Parse(ContestMockedData.IdGossau),
        FileName = "import_gossau_1.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "9f53f1aa-dd60-40b8-b028-3c7686799215",
        },
        EmptyCountingCircles = new List<EmptyImportCountingCircle>
        {
            new EmptyImportCountingCircle
            {
                CountingCircleId = CountingCircleMockedData.Gossau.Id,
            },
        },
        ImportedCountingCircles = new List<ResultImportCountingCircle>
        {
            new ResultImportCountingCircle
            {
                CountingCircleId = CountingCircleMockedData.Gossau.Id,
            },
        },
        ImportType = ResultImportType.EVoting,
    };

    public static IEnumerable<ResultImport> All
    {
        get
        {
            yield return Uzwil1;
            yield return Uzwil2;
            yield return Gossau1;
        }
    }

    public static Task SeedUzwilAggregates(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        return runScoped(async sp =>
        {
            sp.GetRequiredService<IAuthStore>().SetValues(
                "mock-token",
                "fake",
                SecureConnectTestDefaults.MockedTenantUzwil.TenantId,
                [RolesMockedData.MonitoringElectionAdmin]);

            var aggregateRepo = sp.GetRequiredService<IAggregateRepository>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();

            var uzwil1 = Uzwil1;
            var uzwil1Aggregate = aggregateFactory.New<ResultImportAggregate>();
            uzwil1Aggregate.Start(
                uzwil1.FileName,
                uzwil1.ImportType,
                uzwil1.ContestId,
                null,
                "mock-message-id",
                uzwil1.EmptyCountingCircles.Select(x => x.CountingCircleId).ToList(),
                uzwil1.IgnoredCountingCircles);
            uzwil1Aggregate.Complete();

            var importsId = AusmittlungUuidV5.BuildContestImports(uzwil1.ContestId, false);
            var contestAggregate = await aggregateRepo.GetOrCreateById<ContestResultImportsAggregate>(importsId);
            contestAggregate.CreateImport(importsId, uzwil1Aggregate);

            var uzwilDeleted = UzwilDeleted;
            var uzwilDeletedAggregate = aggregateFactory.New<ResultImportAggregate>();
            uzwilDeletedAggregate.DeleteData(uzwilDeleted.ContestId, null, uzwilDeleted.ImportType);
            uzwil1Aggregate.SucceedBy(uzwilDeletedAggregate.Id, false);
            contestAggregate.CreateImport(contestAggregate.Id, uzwilDeletedAggregate);

            var uzwil2 = Uzwil2;
            var uzwil2Aggregate = aggregateFactory.New<ResultImportAggregate>();
            uzwil2Aggregate.Start(
                uzwil2.FileName,
                uzwil2.ImportType,
                uzwil2.ContestId,
                null,
                "mock-message-id",
                uzwil2.EmptyCountingCircles.Select(x => x.CountingCircleId).ToList(),
                uzwil2.IgnoredCountingCircles);
            uzwil2Aggregate.Complete();

            uzwilDeletedAggregate.SucceedBy(uzwil2Aggregate.Id, true);
            contestAggregate.CreateImport(contestAggregate.Id, uzwil2Aggregate);

            await aggregateRepo.Save(contestAggregate);
            await aggregateRepo.Save(uzwil1Aggregate);
            await aggregateRepo.Save(uzwilDeletedAggregate);
            await aggregateRepo.Save(uzwil2Aggregate);
        });
    }

    public static Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        return runScoped(sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            db.AddRange(All);
            return db.SaveChangesAsync();
        });
    }
}
