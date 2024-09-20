// (c) Copyright by Abraxas Informatik AG
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

public class DomainOfInfluencePartyCreateTest : BaseDataProcessorTest
{
    public DomainOfInfluencePartyCreateTest(TestApplicationFactory factory)
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
        var id = Guid.Parse("7808f2f5-80ca-48ec-84ee-a0b376d5bf54");
        await TestEventPublisher.Publish(new DomainOfInfluencePartyCreated
        {
            Party = new DomainOfInfluencePartyEventData
            {
                Id = id.ToString(),
                DomainOfInfluenceId = DomainOfInfluenceMockedData.IdBund,
                Name = { LanguageUtil.MockAllLanguages("Neue Partei") },
                ShortDescription = { LanguageUtil.MockAllLanguages("NP") },
            },
        });

        var parties = await RunOnDb(
            db => db.DomainOfInfluenceParties
            .Where(x => x.BaseDomainOfInfluencePartyId == id)
            .Include(x => x.Translations)
            .ToListAsync(),
            Languages.German);
        parties.Any(p => p.Id == id && p.BaseDomainOfInfluencePartyId == id).Should().BeTrue();

        SetDynamicIdToDefaultValue(parties);

        foreach (var party in parties)
        {
            party.DomainOfInfluenceId = Guid.Empty;
            SetDynamicIdToDefaultValue(party.Translations);
        }

        parties.MatchSnapshot("parties");
    }
}
