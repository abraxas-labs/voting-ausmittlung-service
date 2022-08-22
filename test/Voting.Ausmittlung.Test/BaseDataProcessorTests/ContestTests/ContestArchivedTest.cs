// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestArchivedTest : ContestProcessorBaseTest
{
    public ContestArchivedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestContestArchived()
    {
        await TestEventPublisher.Publish(
            new ContestArchived
            {
                ContestId = ContestMockedData.IdGossau,
            });

        var data = await GetData(c => c.Id == Guid.Parse(ContestMockedData.IdGossau));
        data.Single().State.Should().Be(ContestState.Archived);
    }

    [Fact]
    public async Task TestTransientCatchUpInReplay()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);

        await TestEventPublisher.Publish(
            true,
            new ContestArchived
            {
                ContestId = contestId.ToString(),
            });

        ContestCache.GetAll().Where(c => c.Id == contestId).Should().BeEmpty();
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessing()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);

        await TestEventPublisher.Publish(
            false,
            new ContestArchived
            {
                ContestId = contestId.ToString(),
            });

        ContestCache.GetAll().Where(c => c.Id == contestId).Should().BeEmpty();
        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Any().Should().BeFalse();
    }
}
