// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Jobs;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.BaseDataProcessorTests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.DokConnector.Testing.Service;
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
                .AsSplitQuery()
                .Include(x => x.PoliticalBusinesses)
                .Include(x => x.PoliticalBusinessMetadata)
                .FirstAsync(x => x.ExportConfigurationId == Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf002));

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

        var savedFile = await connectorMock.NextUpload(TimeSpan.FromSeconds(10));

        // This is a CSV export, so we better use the textual representation as snapshot
        new
        {
            savedFile.FileName,
            savedFile.MessageType,
            Data = Encoding.UTF8.GetString(savedFile.Data),
        }.MatchSnapshot();

        await jobTask;
    }

    [Fact]
    public async Task SeantisShouldWork()
    {
        await RunScoped(async (DataContext db) =>
        {
            var item = await db.ResultExportConfigurations
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.PoliticalBusinesses)
                .Include(x => x.PoliticalBusinessMetadata)
                .FirstAsync(x => x.ExportConfigurationId == Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf001));

            item.PoliticalBusinesses = new List<ResultExportConfigurationPoliticalBusiness>
            {
                new()
                {
                    PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen),
                },
                new()
                {
                    PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds),
                },
                new()
                {
                    PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen),
                },
            };

            item.PoliticalBusinessMetadata = new List<ResultExportConfigurationPoliticalBusinessMetadata>
            {
                new()
                {
                    Token = "test",
                    PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen),
                },
                new()
                {
                    Token = "test",
                    PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds),
                },
                new()
                {
                    Token = "different",
                    PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen),
                },
            };

            item.IntervalMinutes = 10;
            item.UpdateNextExecution(MockedClock.UtcNowDate.AddHours(-1));

            await db.SaveChangesAsync();
        });

        var connectorMock = GetService<DokConnectorMock>();

        var job = GetService<ResultExportsJob>();
        var jobTask = job.Run(default);

        var savedFile1 = await connectorMock.NextUpload(TimeSpan.FromSeconds(10));
        var savedFile2 = await connectorMock.NextUpload(TimeSpan.FromSeconds(10));

        // The first zip file is the Gossau proportional election result, which should be "alone" because it has a different token
        // The second zip file should contain the other two proportional election reports grouped together, because the have the same token
        savedFile1.FileName.Should().EndWith(".zip");
        savedFile2.FileName.Should().EndWith(".zip");

        using var ms1 = new MemoryStream(savedFile1.Data);
        using var zip1 = new ZipArchive(ms1);

        using var ms2 = new MemoryStream(savedFile2.Data);
        using var zip2 = new ZipArchive(ms2);

        zip1.Entries.Count.Should().Be(2);
        zip2.Entries.Count.Should().Be(3);

        await jobTask;
    }
}
