// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.ContestTests;

public class ContestPastLockedTest : ContestProcessorBaseTest
{
    private readonly Guid _contestId = Guid.Parse(ContestMockedData.IdGossau);
    private EcdsaPrivateKey? _key;

    public ContestPastLockedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ExportConfigurationMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var item = await db.ResultExportConfigurations
                .AsTracking()
                .Include(x => x.PoliticalBusinesses)
                .FirstAsync(x => x.ExportConfigurationId == Guid.Parse(ExportConfigurationMockedData.IdGossauIntf100));

            item.IntervalMinutes = 10;
            item.UpdateNextExecution(MockedClock.UtcNowDate.AddHours(-1));

            await db.SaveChangesAsync();
        });

        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("test", "test", Enumerable.Empty<string>());

            var asymmetricAlgorithmAdapter = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
            var eventSignatureService = GetService<EventSignatureService>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();

            var aggregate = aggregateFactory.New<ContestEventSignatureAggregate>();

            _key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

            var publicKeyPayload = new PublicKeySignaturePayload(
                EventSignatureVersions.V1,
                _contestId,
                "host",
                _key.Id,
                _key.PublicKey,
                MockedClock.UtcNowDate,
                MockedClock.UtcNowDate);

            aggregate.CreatePublicKey(eventSignatureService.CreatePublicKeySignature(publicKeyPayload));
            await aggregateRepository.Save(aggregate);
        });
    }

    [Fact]
    public async Task TestContestPastLocked()
    {
        SetContestCacheKey(_contestId, _key);

        await TestEventPublisher.Publish(
            new ContestPastLocked
            {
                ContestId = _contestId.ToString(),
            });

        var data = await GetData(c => c.Id == _contestId);
        data.Single().State.Should().Be(ContestState.PastLocked);

        // should unset next executions
        var exportConfig = await RunOnDb(db => db.ResultExportConfigurations
            .FirstAsync(x =>
                x.ContestId == _contestId &&
                x.ExportConfigurationId == Guid.Parse(ExportConfigurationMockedData.IdGossauIntf100)));
        exportConfig.NextExecution.Should().BeNull();
    }

    [Fact]
    public async Task TestTransientCatchUpInReplay()
    {
        SetContestCacheKey(_contestId, null);

        await TestEventPublisher.Publish(
            true,
            new ContestPastLocked
            {
                ContestId = _contestId.ToString(),
            });

        var entry = ContestCache.Get(_contestId);
        entry.State.Should().Be(ContestState.PastLocked);
        entry.KeyData.Should().BeNull();
        entry.MatchSnapshot();

        EventPublisherMock.GetPublishedEvents<EventSignaturePublicKeyDeleted>().Any().Should().BeFalse();
    }

    [Fact]
    public async Task TestTransientCatchUpInLiveProcessing()
    {
        SetContestCacheKey(_contestId, _key);

        await TestEventPublisher.Publish(false, new ContestPastLocked { ContestId = _contestId.ToString() });

        var entry = ContestCache.Get(_contestId);
        entry.State.Should().Be(ContestState.PastLocked);
        entry.KeyData.Should().BeNull();
        entry.MatchSnapshot("cache-entry");

        var ev = EventPublisherMock.GetSinglePublishedEvent<EventSignaturePublicKeyDeleted>();
        ev.KeyId.Should().Be(_key!.Id);
        ev.KeyId = string.Empty;
        ev.MatchSnapshot("event");
    }
}
