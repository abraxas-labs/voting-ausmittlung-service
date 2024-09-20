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

public class ProportionalElectionListUnionUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionListUnionUpdateTest(TestApplicationFactory factory)
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
    public async Task TestListUnionUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionUpdated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = ProportionalElectionMockedData.ListUnionIdStGallenProportionalElectionInContestBund,
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund,
                    Description = { LanguageUtil.MockAllLanguages("Updated list union") },
                },
            },
            new ProportionalElectionListUnionUpdated
            {
                ProportionalElectionListUnion = new ProportionalElectionListUnionEventData
                {
                    Id = ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen,
                    Position = 1,
                    ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
                    Description = { LanguageUtil.MockAllLanguages("Updated sub list union") },
                },
            });

        var idGuid1 = Guid.Parse(ProportionalElectionMockedData.ListUnionIdStGallenProportionalElectionInContestBund);
        var idGuid2 = Guid.Parse(ProportionalElectionMockedData.SubListUnion11IdGossauProportionalElectionInContestStGallen);

        var unions = await RunOnDb(
            db => db.ProportionalElectionListUnions
                .Include(x => x.Translations)
                .Where(x => x.Id == idGuid1 || x.Id == idGuid2)
                .OrderBy(x => x.Position)
                .ToListAsync(),
            Languages.French);

        SetDynamicIdToDefaultValue(unions.SelectMany(u => u.Translations));
        unions.MatchSnapshot();
    }
}
