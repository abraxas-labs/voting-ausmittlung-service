// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ExportConfigurationTests;

public class ExportConfigurationDeleteTest : BaseDataProcessorTest
{
    public ExportConfigurationDeleteTest(TestApplicationFactory factory)
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
    public async Task ShouldDelete()
    {
        var id = Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf001);
        await TestEventPublisher.Publish(new ExportConfigurationDeleted
        {
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdStGallen,
            ConfigurationId = id.ToString(),
        });

        (await RunOnDb(db => db.ExportConfigurations.AnyAsync(x => x.Id == id)))
            .Should()
            .BeFalse();

        var hasResultConfigs = await RunOnDb(db => db
            .ResultExportConfigurations
            .Where(x => x.ExportConfigurationId == id)
            .OrderBy(x => x.Description)
            .AnyAsync());

        // these should still exist on archived contests.
        hasResultConfigs.Should().BeTrue();
    }
}
