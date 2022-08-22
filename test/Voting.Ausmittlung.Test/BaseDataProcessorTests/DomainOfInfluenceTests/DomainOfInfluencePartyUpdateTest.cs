// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.DomainOfInfluenceTests;

public class DomainOfInfluencePartyUpdateTest : BaseDataProcessorTest
{
    public DomainOfInfluencePartyUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldUpdate()
    {
        var id = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundAndere);
        await TestEventPublisher.Publish(new DomainOfInfluencePartyUpdated
        {
            Party = new DomainOfInfluencePartyEventData
            {
                Id = id.ToString(),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
                Name = { LanguageUtil.MockAllLanguages("Andere edited") },
                ShortDescription = { LanguageUtil.MockAllLanguages("AN edited") },
            },
        });

        var parties = await RunOnDb(
            db => db.DomainOfInfluenceParties
            .Where(x => x.BaseDomainOfInfluencePartyId == id)
            .Include(x => x.Translations)
            .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(parties);

        foreach (var party in parties)
        {
            party.DomainOfInfluenceId = Guid.Empty;
            SetDynamicIdToDefaultValue(party.Translations);
        }

        parties.MatchSnapshot("parties");

        // parties from past or active contests shouldn't be updated...
        parties.Any(x => x.Name == "Andere de").Should().BeTrue();
    }
}
