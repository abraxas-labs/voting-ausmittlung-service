// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.EventSignature;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Cryptography.Asymmetric;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test;

public abstract class BaseRestTest : RestAuthorizationBaseTest<TestApplicationFactory, TestStartup>
{
    private Lazy<HttpClient> _monitoringElectionAdminClient;
    private Lazy<HttpClient> _bundMonitoringElectionAdminClient;
    private Lazy<HttpClient> _erfassungElectionAdminClient;
    private int _currentEventNumber;

    protected BaseRestTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetDb();

        TestEventPublisher = GetService<TestEventPublisher>();
        EventPublisherMock = GetService<EventPublisherMock>();
        AggregateRepositoryMock = GetService<AggregateRepositoryMock>();
        EventPublisherMock.Clear();
        AggregateRepositoryMock.Clear();

        ContestCache = GetService<ContestCache>();

        _monitoringElectionAdminClient = new Lazy<HttpClient>(() => CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
            roles: RolesMockedData.MonitoringElectionAdmin));

        _bundMonitoringElectionAdminClient = new Lazy<HttpClient>(() => CreateHttpClient(
            tenant: CountingCircleMockedData.Bund.ResponsibleAuthority.SecureConnectId,
            roles: RolesMockedData.MonitoringElectionAdmin));

        _erfassungElectionAdminClient = new Lazy<HttpClient>(() => CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
            roles: RolesMockedData.ErfassungElectionAdmin));

        EventInfoProvider = GetService<EventInfoProvider>();

        MessagingTestHarness = GetService<InMemoryTestHarness>();
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected ContestCache ContestCache { get; }

    protected TestEventPublisher TestEventPublisher { get; }

    protected HttpClient MonitoringElectionAdminClient => _monitoringElectionAdminClient.Value;

    protected HttpClient BundMonitoringElectionAdminClient => _bundMonitoringElectionAdminClient.Value;

    protected HttpClient ErfassungElectionAdminClient => _erfassungElectionAdminClient.Value;

    protected EventInfoProvider EventInfoProvider { get; }

    protected InMemoryTestHarness MessagingTestHarness { get; set; }

    /// <summary>
    /// Authorized roles should have access to the method.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Fact]
    public async Task AuthorizedRolesShouldHaveAccess()
    {
        await ModifyDbEntities<SimplePoliticalBusiness>(
            _ => true,
            pb => pb.EndResultFinalized = true);
        foreach (var role in AuthorizedRoles())
        {
            var response = await AuthorizationTestCall(CreateHttpClient(role == NoRole ? Array.Empty<string>() : new[] { role }));
            response.IsSuccessStatusCode.Should().BeTrue($"{role} should have access");
        }
    }

    protected abstract IEnumerable<string> AuthorizedRoles();

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        return RolesMockedData
            .All()
            .Append(NoRole)
            .Except(AuthorizedRoles());
    }

    protected Task RunOnDb(Func<DataContext, Task> action)
        => RunScoped(action);

    protected Task<TResult> RunOnDb<TResult>(Func<DataContext, Task<TResult>> action, string? language = null)
    {
        return RunScoped<DataContext, TResult>(db =>
        {
            db.Language = language;
            return action(db);
        });
    }

    protected Task ModifyDbEntities<T>(Expression<Func<T, bool>> predicate, Action<T> modifier, bool splitQuery = false)
        where T : class
    {
        return RunOnDb(async db =>
        {
            var query = db.Set<T>().AsTracking();
            if (splitQuery)
            {
                query = query.AsSplitQuery();
            }

            var entities = await query.Where(predicate).ToListAsync();
            foreach (var entity in entities)
            {
                modifier(entity);
            }

            await db.SaveChangesAsync();
        });
    }

    protected async Task<T?> ReadJson<T>(HttpResponseMessage response)
    {
        var jsonOpts = GetService<IOptions<JsonOptions>>();
        return await JsonSerializer.DeserializeAsync<T>(
            await response.Content.ReadAsStreamAsync(),
            jsonOpts.Value.JsonSerializerOptions);
    }

    protected async Task<ProblemDetails> AssertProblemDetails(
        Func<Task<HttpResponseMessage>> testCode,
        HttpStatusCode statusCode,
        string? detailContains = null)
    {
        var response = await AssertStatus(testCode, statusCode);
        var problemDetails = await ReadJson<ProblemDetails>(response);
        if (detailContains != null)
        {
            problemDetails!.Detail.Should().Contain(detailContains);
        }

        return problemDetails!;
    }

    protected T AssertException<T>(
        Action testCode,
        string messageContains)
        where T : Exception
    {
        var ex = Assert.Throws<T>(testCode);
        ex.Message.Should().Contain(messageContains);
        return ex;
    }

    protected async Task<T> AssertException<T>(
        Func<Task> testCode,
        string messageContains)
        where T : Exception
    {
        var ex = await Assert.ThrowsAsync<T>(testCode);
        ex.Message.Should().Contain(messageContains);
        return ex;
    }

    protected void ResetDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }

    protected async Task TestEventWithSignature(
        string contestId,
        Func<Task<EventWithMetadata>> testAction)
    {
        await TestEventsWithSignature(contestId, async () => [await testAction()]);
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

    protected async Task AssertHasPublishedEventProcessedMessage(MessageDescriptor eventDescriptor, Guid entityId)
    {
        await AssertHasPublishedMessage<EventProcessedMessage>(
            x => eventDescriptor.FullName == x.EventType && x.EntityId == entityId);
    }

    protected async Task AssertHasPublishedMessage<T>(Func<T, bool> predicate, bool hasMessage = true)
        where T : class
    {
        var hasOne = await MessagingTestHarness.Published.Any<T>(x => predicate(x.Context.Message));
        hasOne.Should().Be(hasMessage);
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
                Seconds = 1594980476,
            },
            Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
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
