// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Abraxas.Voting.Basis.Shared.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ExportConfigurationTests;

public class ExportConfigurationUpdateTest : BaseDataProcessorTest
{
    public ExportConfigurationUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ExportConfigurationMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldUpdate()
    {
        var id = Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf001);
        await TestEventPublisher.Publish(new ExportConfigurationUpdated
        {
            Configuration = new ExportConfigurationEventData
            {
                Id = id.ToString(),
                Description = "Intf001-updated",
                ExportKeys =
                {
                    AusmittlungWabstiCTemplates.SGGemeinden.Key,
                    AusmittlungWabstiCTemplates.WMWahl.Key,
                    AusmittlungWabstiCTemplates.WPKandidaten.Key,
                },
                EaiMessageType = "1234-updated",
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
                Provider = ExportProvider.Unspecified, // should get converted to Standard
            },
        });

        var config = await RunOnDb(db => db.ExportConfigurations.FirstAsync(x => x.Id == id));
        config.MatchSnapshot("config");

        var resultConfigs = await RunOnDb(db => db
            .ResultExportConfigurations
            .Where(x => x.ExportConfigurationId == id)
            .OrderBy(x => x.Description)
            .ThenBy(x => x.ContestId)
            .ToListAsync());

        // past contests shouldn't be updated...
        resultConfigs.Any(x => x.EaiMessageType != "1234-updated").Should().BeTrue();

        foreach (var resultConfig in resultConfigs)
        {
            resultConfig.DomainOfInfluenceId = Guid.Empty;
        }

        resultConfigs.MatchSnapshot("resultConfigs");
    }
}
