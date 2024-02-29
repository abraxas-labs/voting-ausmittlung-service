// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluencePartyDeleteTest : BaseDataProcessorTest
{
    public DomainOfInfluencePartyDeleteTest(TestApplicationFactory factory)
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
        var id = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundAndere);
        await TestEventPublisher.Publish(new DomainOfInfluencePartyDeleted
        {
            Id = id.ToString(),
            DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
        });

        var parties = await RunOnDb(
            db => db.DomainOfInfluenceParties
            .IgnoreQueryFilters()
            .Where(x => x.BaseDomainOfInfluencePartyId == id)
            .ToListAsync());

        parties.Any(x => x.Deleted).Should().BeTrue();

        // parties from active or past contests shouldn't be deleted...
        parties.Any(x => x.Deleted).Should().BeTrue();
    }
}
