// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Jobs;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.BaseDataProcessorTests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Mocks;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.JobTests;

public class ResultExportsJobTest : BaseDataProcessorTest
{
    public ResultExportsJobTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await ExportConfigurationMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        await RunScoped(async (DataContext db) =>
        {
            var item = await db.ResultExportConfigurations
                .AsTracking()
                .Include(x => x.PoliticalBusinesses)
                .FirstAsync(x => x.ExportConfigurationId == Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf001));

            item.PoliticalBusinesses!.Clear();
            item.PoliticalBusinesses.Add(new ResultExportConfigurationPoliticalBusiness
            {
                PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen),
            });

            item.IntervalMinutes = 10;
            item.UpdateNextExecution(MockedClock.UtcNowDate.AddHours(-1));

            await db.SaveChangesAsync();
        });

        var connectorMock = GetService<DokConnectorMock>();

        var job = GetService<ResultExportsJob>();
        var jobTask = job.Run(default);

        var savedFile = await connectorMock.WaitForNextSave();
        savedFile.MatchSnapshot();

        await jobTask;
    }
}
