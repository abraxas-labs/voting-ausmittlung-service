// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionListUnionMainListUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionListUnionMainListUpdateTest(TestApplicationFactory factory)
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
    public async Task TestListUnionMainListUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionListUnionMainListUpdated
            {
                ProportionalElectionListUnionId = ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen,
                ProportionalElectionMainListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
            });

        var idGuid = Guid.Parse(ProportionalElectionMockedData.SubListUnion21IdGossauProportionalElectionInContestStGallen);

        var union = await RunOnDb(
            db => db.ProportionalElectionListUnions
                .Include(x => x.Translations)
                .FirstAsync(x => x.Id == idGuid),
            Languages.German);

        SetDynamicIdToDefaultValue(union.Translations);
        union.MatchSnapshot();
    }
}
