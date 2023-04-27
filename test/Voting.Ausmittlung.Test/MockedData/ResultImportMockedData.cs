// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Testing.Mocks;
using ResultImport = Voting.Ausmittlung.Data.Models.ResultImport;
using User = Voting.Ausmittlung.Data.Models.User;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ResultImportMockedData
{
    public static ResultImport Uzwil1 => new ResultImport
    {
        Completed = true,
        Id = Guid.Parse("d02ad8dc-2372-4f4e-afb7-9b55c6a512ae"),
        Started = MockedClock.GetDate(-10),
        ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting),
        FileName = "import_uzwil_1.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "c94eb4da-cf1a-4fb1-bad3-113fd9750579",
        },
    };

    public static ResultImport UzwilDeleted => new ResultImport
    {
        Id = Guid.Parse("57fdd782-3185-435d-8d84-95bc9f0b378d"),
        Started = MockedClock.GetDate(-5),
        Deleted = true,
        ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting),
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "c94eb4da-cf1a-4fb1-bad3-113fd9750579",
        },
    };

    public static ResultImport Uzwil2 => new ResultImport
    {
        Id = Guid.Parse("e45540a9-d412-4cc2-a219-12b052a2b58e"),
        Started = MockedClock.GetDate(-2),
        ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting),
        FileName = "import_uzwil_2.xml",
        StartedBy = new User
        {
            FirstName = "Hans",
            LastName = "Meier",
            SecureConnectId = "c94eb4da-cf1a-4fb1-bad3-113fd9750579",
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
