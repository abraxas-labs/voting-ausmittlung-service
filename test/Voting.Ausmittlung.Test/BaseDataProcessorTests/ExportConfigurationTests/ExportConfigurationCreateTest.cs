// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ExportConfigurationTests;

public class ExportConfigurationCreateTest : BaseDataProcessorTest
{
    public ExportConfigurationCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldCreate()
    {
        var id = Guid.Parse("47d726a6-4a3f-4b84-a9d7-abe8383f89b2");
        await TestEventPublisher.Publish(new ExportConfigurationCreated
        {
            Configuration = new ExportConfigurationEventData
            {
                Id = id.ToString(),
                Description = "Intf001",
                ExportKeys =
                {
                    AusmittlungWabstiCTemplates.SGGemeinden.Key,
                    AusmittlungWabstiCTemplates.WMWahl.Key,
                    AusmittlungWabstiCTemplates.WPKandidaten.Key,
                },
                EaiMessageType = "1234",
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                Provider = ExportProvider.Seantis,
            },
        });

        var config = await RunOnDb(db => db.ExportConfigurations.FirstAsync(x => x.Id == id));
        config.MatchSnapshot("config");

        var resultConfigs = await RunOnDb(db => db
            .ResultExportConfigurations
            .Include(x => x.ExportConfiguration)
            .Where(x => x.ExportConfigurationId == id)
            .OrderBy(x => x.ExportConfiguration!.Description)
            .ThenBy(x => x.ContestId)
            .ToListAsync());

        foreach (var resultConfig in resultConfigs)
        {
            resultConfig.ExportConfiguration!.DomainOfInfluenceId = Guid.Empty;
            resultConfig.ExportConfiguration!.ResultExportConfigurations = [];
        }

        resultConfigs.MatchSnapshot("resultConfigs");
    }

    [Fact]
    public async Task ShouldCreateSedex()
    {
        var id = Guid.Parse("47d726a6-4a3f-4b84-a9d7-abe8383f89b3");
        await TestEventPublisher.Publish(new ExportConfigurationCreated
        {
            Configuration = new ExportConfigurationEventData
            {
                Id = id.ToString(),
                Description = "Intf003",
                ExportKeys =
                {
                    AusmittlungWabstiCTemplates.SGGemeinden.Key,
                    AusmittlungWabstiCTemplates.WMWahl.Key,
                    AusmittlungWabstiCTemplates.WPKandidaten.Key,
                },
                EaiMessageType = "23456",
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                Provider = ExportProvider.Sedex,
                SedexConfiguration = new SedexConfigurationEventData
                {
                    SenderId = "1-ABX-1",
                    RecipientIds = { "2-ABX-2", "3-ABX-3" },
                },
            },
        });

        var config = await RunOnDb(db => db.ExportConfigurations.FirstAsync(x => x.Id == id));
        config.MatchSnapshot();
    }
}
