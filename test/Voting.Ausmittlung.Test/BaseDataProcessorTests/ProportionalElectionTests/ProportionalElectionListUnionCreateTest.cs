// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionListUnionCreateTest : BaseDataProcessorTest
{
    public ProportionalElectionListUnionCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestListUnionCreate()
    {
        var id1 = Guid.Parse("0c9d4223-25f3-4c55-bcf0-fcb952eff9da");
        var id2 = Guid.Parse("7ea31730-0427-4a8c-a8aa-4f4323d911c2");
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionCreated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = id1.ToString(),
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    Description = { LanguageUtil.MockAllLanguages("Created list union") },
                },
            },
            new ProportionalElectionListUnionCreated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = id2.ToString(),
                    Position = 2,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallenWithoutChilds,
                    Description = { LanguageUtil.MockAllLanguages("Created list union 2") },
                },
            });

        var unions = await RunOnDb(
            db => db.ProportionalElectionListUnions
                .Include(u => u.Translations)
                .Where(x => x.Id == id1 || x.Id == id2)
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.German);

        SetDynamicIdToDefaultValue(unions.SelectMany(u => u.Translations));
        unions.MatchSnapshot();
    }
}
