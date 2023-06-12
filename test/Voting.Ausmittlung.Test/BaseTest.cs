// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Database.Models;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Contest = Voting.Ausmittlung.Data.Models.Contest;

namespace Voting.Ausmittlung.Test;

public abstract class BaseTest<TService> : GrpcAuthorizationBaseTest<TestApplicationFactory, TestStartup>
    where TService : ClientBase<TService>
{
    private readonly Lazy<TService> _stGallenErfassungElectionAdminClient;
    private readonly Lazy<TService> _stGallenMonitoringElectionAdminClient;

    private int _currentEventNumber;
    private long _mockedEventInfoSeconds;

    protected BaseTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();

        TestEventPublisher = GetService<TestEventPublisher>();
        EventPublisherMock = GetService<EventPublisherMock>();
        EventPublisherMock.Clear();

        AggregateRepositoryMock = GetService<AggregateRepositoryMock>();
        AggregateRepositoryMock.Clear();

        ContestCache = GetService<ContestCache>();

        ErfassungCreatorClient = CreateService(RolesMockedData.ErfassungCreator);
        ErfassungElectionAdminClient = CreateService(RolesMockedData.ErfassungElectionAdmin);
        MonitoringElectionAdminClient = CreateService(RolesMockedData.MonitoringElectionAdmin);
        _stGallenMonitoringElectionAdminClient = new Lazy<TService>(() =>
            CreateServiceWithTenant(SecureConnectTestDefaults.MockedTenantStGallen.Id, RolesMockedData.MonitoringElectionAdmin));
        _stGallenErfassungElectionAdminClient = new Lazy<TService>(() =>
            CreateServiceWithTenant(SecureConnectTestDefaults.MockedTenantStGallen.Id, RolesMockedData.ErfassungElectionAdmin));

        MessagingTestHarness = GetService<InMemoryTestHarness>();
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected TestEventPublisher TestEventPublisher { get; }

    protected ContestCache ContestCache { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected TService ErfassungCreatorClient { get; }

    protected TService ErfassungElectionAdminClient { get; }

    protected TService MonitoringElectionAdminClient { get; }

    protected TService StGallenMonitoringElectionAdminClient => _stGallenMonitoringElectionAdminClient.Value;

    protected TService StGallenErfassungElectionAdminClient => _stGallenErfassungElectionAdminClient.Value;

    protected InMemoryTestHarness MessagingTestHarness { get; set; }

    protected async Task AssertHasPublishedMessage<T>(Func<T, bool> predicate, bool hasMessage = true)
        where T : class
    {
        var hasOne = await MessagingTestHarness.Published.Any<T>(x => predicate(x.Context.Message));
        hasOne.Should().Be(hasMessage);
    }

    protected Task PublishMessage<T>(T msg) => GetService<IBus>().Publish(msg);

    protected Task RunOnDb(Func<DataContext, Task> action, string? language = null)
    {
        return RunScoped<DataContext>(db =>
        {
            db.Language = language;
            return action(db);
        });
    }

    protected Task<TResult> RunOnDb<TResult>(Func<DataContext, Task<TResult>> action, string? language = null)
    {
        return RunScoped<DataContext, TResult>(db =>
        {
            db.Language = language;
            return action(db);
        });
    }

    protected Task ModifyDbEntities<T>(Expression<Func<T, bool>> predicate, Action<T> modifier)
        where T : class
    {
        return RunOnDb(async db =>
        {
            var entities = await db.Set<T>().AsTracking().Where(predicate).ToListAsync();
            foreach (var entity in entities)
            {
                modifier(entity);
            }

            await db.SaveChangesAsync();
        });
    }

    protected Task SetContestState(string id, Data.Models.ContestState state)
        => ModifyDbEntities<Contest>(c => c.Id == Guid.Parse(id), c => c.State = state);

    protected async Task TestEventWithSignature(
        string contestId,
        Func<Task<EventWithMetadata>> testAction)
    {
        await TestEventsWithSignature(contestId, async () => new[] { await testAction() });
    }

    protected async Task TestEventsWithSignature(string contestId, Func<Task<EventWithMetadata[]>> testAction)
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            var asymmetricAlgorithmAdapter = sp.GetRequiredService<IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey>>();
            var key = asymmetricAlgorithmAdapter.CreateRandomPrivateKey();

            var entry = ContestCache.Get(Guid.Parse(contestId));
            entry.KeyData = new ContestCacheEntryKeyData(key, DateTime.MinValue, DateTime.MaxValue);

            var events = await testAction();

            foreach (var ev in events)
            {
                EnsureEventSignatureMetadataCorrectlyCreated(ev, contestId, key.Id);
            }

            entry.KeyData = null;
        });
    }

    protected void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }

    protected TService CreateService(
        string tenantId = TestDefaults.TenantId,
        string userId = TestDefaults.UserId,
        params string[] roles)
    {
        return (TService)Activator.CreateInstance(typeof(TService), CreateGrpcChannel(true, tenantId, userId, roles))!;
    }

    protected int GetNextEventNumber()
    {
        return _currentEventNumber++;
    }

    protected EventInfo GetMockedEventInfo()
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980476 + _mockedEventInfoSeconds++,
            },
            Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
    }

    protected async Task RunEvents<TEvent>(bool clear = true)
        where TEvent : IMessage<TEvent>
    {
        var events = EventPublisherMock.GetPublishedEvents<TEvent>().ToArray();
        await TestEventPublisher.Publish(
            _currentEventNumber,
            events);
        _currentEventNumber += events.Length;
        if (clear)
        {
            EventPublisherMock.Clear();
        }
    }

    protected void SetDynamicIdToDefaultValue(IEnumerable<BaseEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.Id = Guid.Empty;
        }
    }

    protected void TrySetFakeAuth(string tenantId, params string[] roles)
    {
        if (GetService<IAuth>().IsAuthenticated)
        {
            return;
        }

        GetService<IAuthStore>().SetValues(
            "mock-token",
            "fake",
            tenantId,
            roles);
    }

    protected TService CreateServiceWithTenant(string tenantId, params string[] roles)
    {
        return (TService)Activator.CreateInstance(typeof(TService), CreateGrpcChannel(true, tenantId, "default-user-id", roles))!;
    }

    private TService CreateService(params string[] roles)
    {
        return (TService)Activator.CreateInstance(typeof(TService), CreateGrpcChannel(roles))!;
    }

    private void EnsureEventSignatureMetadataCorrectlyCreated(EventWithMetadata ev, string contestId, string keyId)
    {
        var eventSignatureMetadata = ev.Metadata as Abraxas.Voting.Ausmittlung.Events.V1.Metadata.EventSignatureBusinessMetadata;
        eventSignatureMetadata.Should().NotBeNull();

        eventSignatureMetadata!.ContestId.Should().Be(contestId);
        eventSignatureMetadata.KeyId.Should().Be(keyId);
        eventSignatureMetadata.HostId.Should().NotBeEmpty();
        eventSignatureMetadata.Signature.Should().NotBeEmpty();
    }
}
