// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionUnionTests;

public class MajorityElectionUnionCreateTest : BaseDataProcessorTest
{
    public MajorityElectionUnionCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestCreate()
    {
        var newId = Guid.Parse("bb092abb-3f8b-4a06-8169-a738d1369fd0");

        await TestEventPublisher.Publish(
            new MajorityElectionUnionCreated
            {
                MajorityElectionUnion = new MajorityElectionUnionEventData
                {
                    Id = newId.ToString(),
                    Description = "new description",
                    ContestId = ContestMockedData.IdStGallenEvoting,
                },
            });

        var result = await RunOnDb(db => db.MajorityElectionUnions.FirstOrDefaultAsync(u => u.Id == newId));
        result!.MatchSnapshot(x => x.Id);
    }
}
