// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
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

        // virtual call in ctor should be ok for tests
        MonitoringElectionAdminClient = CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
            roles: RolesMockedData.MonitoringElectionAdmin);

        BundMonitoringElectionAdminClient = CreateHttpClient(
            tenant: CountingCircleMockedData.Bund.ResponsibleAuthority.SecureConnectId,
            roles: RolesMockedData.MonitoringElectionAdmin);

        ErfassungElectionAdminClient = CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
            roles: RolesMockedData.ErfassungElectionAdmin);

        EventInfoProvider = GetService<EventInfoProvider>();

        MessagingTestHarness = GetService<InMemoryTestHarness>();
    }

    protected EventPublisherMock EventPublisherMock { get; }

    protected AggregateRepositoryMock AggregateRepositoryMock { get; }

    protected ContestCache ContestCache { get; }

    protected TestEventPublisher TestEventPublisher { get; }

    protected HttpClient MonitoringElectionAdminClient { get; }

    protected HttpClient BundMonitoringElectionAdminClient { get; }

    protected HttpClient ErfassungElectionAdminClient { get; }

    protected EventInfoProvider EventInfoProvider { get; }

    protected InMemoryTestHarness MessagingTestHarness { get; set; }

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

    protected async Task AssertHasPublishedMessage<T>(Func<T, bool> predicate, bool hasMessage = true)
        where T : class
    {
        var hasOne = await MessagingTestHarness.Published.Any<T>(x => predicate(x.Context.Message));
        hasOne.Should().Be(hasMessage);
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
