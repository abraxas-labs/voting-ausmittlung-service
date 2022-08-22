// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.MajorityElectionUnionTests;

public class MajorityElectionUnionUpdateTest : BaseDataProcessorTest
{
    public MajorityElectionUnionUpdateTest(TestApplicationFactory factory)
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
    public async Task TestUpdate()
    {
        await TestEventPublisher.Publish(
            new MajorityElectionUnionUpdated
            {
                MajorityElectionUnion = new MajorityElectionUnionEventData
                {
                    Id = MajorityElectionUnionMockedData.IdStGallen1,
                    Description = "edited description",
                    ContestId = ContestMockedData.IdStGallenEvoting,
                },
            });

        var result = await RunOnDb(db => db.MajorityElectionUnions
            .Include(u => u.MajorityElectionUnionEntries)
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(MajorityElectionUnionMockedData.IdStGallen1)));

        result.Should().NotBeNull();
        foreach (var entry in result!.MajorityElectionUnionEntries)
        {
            entry.MajorityElectionUnion = null!;
        }

        result.MatchSnapshot();
    }
}
