// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.EventSignatureTests;

public class EventSignatureServiceTest : BaseIntegrationTest
{
    private readonly Guid _contestId = ContestMockedData.GuidGossau;

    public EventSignatureServiceTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await ContestMockedData.Seed(RunScoped);
    }

    [Fact]
    public void BuildBusinessMetadataShouldReturnMetadata()
    {
        var asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
        var key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

        var keyData = new ContestCacheEntryKeyData(key, MockedClock.UtcNowDate.AddDays(-1), MockedClock.UtcNowDate.AddDays(1));
        var contest = ContestCache.Get(_contestId);
        contest.KeyData = keyData;

        var service = GetService<EventSignatureService>();
        var eventData = new VoteResultBundleDeleted { EventInfo = new EventInfo { Timestamp = Timestamp.FromDateTime(MockedClock.UtcNowDate) } };

        var metadata = service.BuildBusinessMetadata("stream", eventData, _contestId, Guid.NewGuid());
        metadata.ContestId.Should().Be(_contestId.ToString());
        metadata.Signature.Should().NotBeNullOrEmpty();
        metadata.KeyId.Should().Be(key.Id);
        metadata.HostId.Should().NotBeEmpty();
        metadata.SignatureVersion.Should().NotBe(0);
    }

    [Fact]
    public void BuildBusinessMetadataShouldReturnEmptyMetadataWhenKeyIsExpired()
    {
        var asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
        var key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

        var keyData = new ContestCacheEntryKeyData(key, MockedClock.UtcNowDate.AddDays(-1), MockedClock.UtcNowDate.AddMinutes(-1));
        var contest = ContestCache.Get(_contestId);
        contest.KeyData = keyData;

        var service = GetService<EventSignatureService>();
        var eventData = new VoteResultBundleDeleted { EventInfo = new EventInfo { Timestamp = Timestamp.FromDateTime(MockedClock.UtcNowDate) } };

        var metadata = service.BuildBusinessMetadata("stream", eventData, _contestId, Guid.NewGuid());
        metadata.ContestId.Should().Be(_contestId.ToString());
        metadata.Signature.Should().BeEmpty();
        metadata.KeyId.Should().BeNullOrEmpty();
        metadata.HostId.Should().BeNullOrEmpty();
        metadata.SignatureVersion.Should().Be(0);
    }

    [Fact]
    public void BuildBusinessMetadataShouldReturnEmptyMetadataWhenKeyIsNull()
    {
        var contest = ContestCache.Get(_contestId);
        contest.KeyData = null;

        var service = GetService<EventSignatureService>();
        var eventData = new VoteResultBundleDeleted { EventInfo = new EventInfo { Timestamp = Timestamp.FromDateTime(MockedClock.UtcNowDate) } };

        var metadata = service.BuildBusinessMetadata("stream", eventData, _contestId, Guid.NewGuid());
        metadata.ContestId.Should().Be(_contestId.ToString());
        metadata.Signature.Should().BeEmpty();
        metadata.KeyId.Should().BeNullOrEmpty();
        metadata.HostId.Should().BeNullOrEmpty();
        metadata.SignatureVersion.Should().Be(0);
    }

    [Fact]
    public void IsExpiredShouldReturnCorrectValue()
    {
        var asymmetricAlgorithmAdapter = GetService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
        var key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

        var keyData = new ContestCacheEntryKeyData(key, MockedClock.UtcNowDate.AddDays(-1), MockedClock.UtcNowDate.AddHours(1));
        keyData.IsExpired(MockedClock.UtcNowDate).Should().BeFalse();
        keyData.IsExpired(MockedClock.UtcNowDate.AddHours(2)).Should().BeTrue();
    }
}
