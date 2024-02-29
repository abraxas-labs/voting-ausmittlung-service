// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ProportionalElectionTests;

public class ProportionalElectionCandidateDeleteTest : BaseDataProcessorTest
{
    public ProportionalElectionCandidateDeleteTest(TestApplicationFactory factory)
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
    public async Task TestCandidateDelete()
    {
        await TestEventPublisher.Publish(new ProportionalElectionCandidateDeleted
        {
            ProportionalElectionCandidateId = ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen,
        });

        var idGuid = Guid.Parse(ProportionalElectionMockedData
            .CandidateIdStGallenProportionalElectionInContestStGallen);
        var item = await RunOnDb(db => db.ProportionalElections
            .FirstOrDefaultAsync(c => c.Id == idGuid));
        item.Should().BeNull();
    }
}
