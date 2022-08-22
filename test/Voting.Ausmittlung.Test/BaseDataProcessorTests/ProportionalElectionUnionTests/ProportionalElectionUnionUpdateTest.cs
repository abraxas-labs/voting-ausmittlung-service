// (c) Copyright 2022 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionUnionTests;

public class ProportionalElectionUnionUpdateTest : BaseDataProcessorTest
{
    public ProportionalElectionUnionUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestUpdate()
    {
        await TestEventPublisher.Publish(
            new ProportionalElectionUnionUpdated
            {
                ProportionalElectionUnion = new ProportionalElectionUnionEventData
                {
                    Id = ProportionalElectionUnionMockedData.IdStGallen1,
                    Description = "edited description",
                    ContestId = ContestMockedData.IdStGallenEvoting,
                },
            });

        var result = await RunOnDb(
            db => db.ProportionalElectionUnions
                .AsSplitQuery()
                .Include(u => u.ProportionalElectionUnionEntries)
                .Include(u => u.ProportionalElectionUnionLists)
                .ThenInclude(u => u.Translations)
                .FirstOrDefaultAsync(u => u.Id == Guid.Parse(ProportionalElectionUnionMockedData.IdStGallen1)),
            Languages.German);

        SetDynamicIdToDefaultValue(result!.ProportionalElectionUnionLists.SelectMany(x => x.Translations));
        result.MatchSnapshot();
    }
}
