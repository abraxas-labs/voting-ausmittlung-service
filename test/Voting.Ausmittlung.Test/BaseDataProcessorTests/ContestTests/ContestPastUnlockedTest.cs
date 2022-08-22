// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Google.Protobuf;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestPastUnlockedTest : ContestProcessorBaseTest
{
    public ContestPastUnlockedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestContestPastUnlocked()
    {
        await TestEventPublisher.Publish(
            new ContestPastUnlocked
            {
                ContestId = ContestMockedData.IdGossau,
                EventInfo = GetMockedEventInfo(),
            });

        var data = await GetData(c => c.Id == Guid.Parse(ContestMockedData.IdGossau));
        data.Single().State.Should().Be(ContestState.PastUnlocked);
    }

    [Fact]
    public async Task TestTransientCatchUpInReplay()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);

        await TestEventPublisher.Publish(
            true,
            new ContestPastUnlocked
            {
                ContestId = contestId.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var entry = ContestCache.Get(contestId);
        entry.State.Should().Be(ContestState.PastUnlocked);
        entry.KeyData.Should().BeNull();
        entry.MatchSnapshot();

        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeySigned>().Should().HaveCount(0);
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessing()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);

        var entry = ContestCache.Get(contestId);
        entry.KeyData = null;

        await TestEventPublisher.Publish(
            false,
            new ContestPastUnlocked
            {
                ContestId = contestId.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        entry.State.Should().Be(ContestState.PastUnlocked);
        entry.KeyData.Should().NotBeNull();
        entry.KeyData!.Key.Id.Should().NotBeNullOrWhiteSpace();
        entry.KeyData.Key.PublicKey.Should().NotBeNullOrEmpty();
        entry.KeyData.Key.PrivateKey.Should().NotBeNullOrEmpty();
        entry.KeyData = null;
        entry.MatchSnapshot("cache-entry");

        var ev = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeySigned>();
        ev.HsmSignature.Should().NotBeEmpty();
        ev.KeyId.Should().NotBeNullOrWhiteSpace();
        ev.HsmSignature = ByteString.Empty;
        ev.KeyId = string.Empty;
        ev.MatchSnapshot("event");
    }
}
