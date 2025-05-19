// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Models.Import;
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

public static class ResultImportECountingMockedData
{
    public static ResultImport Uzwil1 => new ResultImport
    {
        Completed = true,
        Id = Guid.Parse("9aeb531f-3e33-4277-a656-9df549491095"),
        Started = MockedClock.GetDate(-10),
        ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
        CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwil),
        FileName = "import_uzwil_1.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "973ed91e-9d22-4f4e-ae32-7b58c7ed25fb",
        },
        ImportType = ResultImportType.ECounting,
    };

    public static ResultImport UzwilDeleted => new ResultImport
    {
        Id = Guid.Parse("aa455a50-f59c-48a1-b1a7-1328e1a67722"),
        Started = MockedClock.GetDate(-5),
        Deleted = true,
        ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
        CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwil),
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "66c5e9ff-efd5-4d5e-90fc-1ff54505802e",
        },
        ImportType = ResultImportType.ECounting,
    };

    public static ResultImport Uzwil2 => new ResultImport
    {
        Id = Guid.Parse("49c00955-b97d-486a-bc30-594fa6c65c6c"),
        Started = MockedClock.GetDate(-2),
        ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
        CountingCircleId = Guid.Parse(CountingCircleMockedData.IdUzwil),
        FileName = "import_uzwil_2.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "fc8e9579-baee-46c1-8c61-46ac30f1b733",
        },
        ImportType = ResultImportType.ECounting,
    };

    public static ResultImport Gossau1 => new ResultImport
    {
        Id = Guid.Parse("d61f4474-bfe8-496a-a64f-a8304804d24a"),
        Started = MockedClock.GetDate(-5),
        ContestId = Guid.Parse(ContestMockedData.IdGossau),
        CountingCircleId = Guid.Parse(CountingCircleMockedData.IdGossau),
        FileName = "import_gossau_1.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "55ddd391-b277-45a3-9fbb-46efae477e92",
        },
        ImportType = ResultImportType.ECounting,
    };

    public static IEnumerable<ResultImport> All
    {
        get
        {
            yield return Uzwil1;
            yield return UzwilDeleted;
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
                uzwil1.CountingCircleId,
                "mock-message-id",
                uzwil1.IgnoredCountingCircles);
            uzwil1Aggregate.Complete();

            var importsId = AusmittlungUuidV5.BuildContestCountingCircleImports(uzwil1.ContestId, uzwil1.CountingCircleId!.Value, false);
            var importsAggregate = await aggregateRepo.GetOrCreateById<CountingCircleResultImportsAggregate>(importsId);
            importsAggregate.CreateImport(importsId, uzwil1Aggregate);

            var uzwilDeleted = UzwilDeleted;
            var uzwilDeletedAggregate = aggregateFactory.New<ResultImportAggregate>();
            uzwilDeletedAggregate.DeleteData(uzwilDeleted.ContestId, null, uzwilDeleted.ImportType);
            uzwil1Aggregate.SucceedBy(uzwilDeletedAggregate.Id, false);
            importsAggregate.CreateImport(importsAggregate.Id, uzwilDeletedAggregate);

            var uzwil2 = Uzwil2;
            var uzwil2Aggregate = aggregateFactory.New<ResultImportAggregate>();
            uzwil2Aggregate.Start(
                uzwil2.FileName,
                uzwil2.ImportType,
                uzwil2.ContestId,
                uzwil2.CountingCircleId,
                "mock-message-id",
                uzwil2.IgnoredCountingCircles);
            uzwil2Aggregate.ImportVoteResult(new VoteResultImport(Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen), CountingCircleMockedData.GuidUzwil, 10));
            uzwil2Aggregate.Complete();

            uzwilDeletedAggregate.SucceedBy(uzwil2Aggregate.Id, true);
            importsAggregate.CreateImport(importsAggregate.Id, uzwil2Aggregate);

            await aggregateRepo.Save(importsAggregate);
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
