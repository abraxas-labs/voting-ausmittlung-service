// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Shared.V1;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Mocks;
using Xunit;

namespace Voting.Ausmittlung.Test.ServiceModes;

public abstract class BaseServiceModeTest<TFactory> : BaseTest<TFactory, ServiceModeAppStartup>
    where TFactory : BaseTestApplicationFactory<ServiceModeAppStartup>
{
    private readonly ServiceMode _serviceMode;

    protected BaseServiceModeTest(TFactory factory, ServiceMode serviceMode)
        : base(factory)
    {
        _serviceMode = serviceMode;
    }

    [Fact]
    public async Task WriteEndpointShouldWorkIfPublisher()
    {
        if (!_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        var eventPublisherMock = GetService<EventPublisherMock>();
        eventPublisherMock.Clear();

        DatabaseUtil.Truncate(GetService<DataContext>());

        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) => permissionBuilder.RebuildPermissionTree());

        using var channel = CreateGrpcChannel(tenant: SecureConnectTestDefaults.MockedTenantGossau.Id, roles: RolesMockedData.ErfassungElectionAdmin);
        var client = new ResultService.ResultServiceClient(channel);
        await client.GetListAsync(new()
        {
            ContestId = ContestMockedData.IdGossau,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });

        var createdEvent = eventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionStarted>();
        createdEvent.VoteResultId.Should().Be(VoteResultMockedData.IdGossauVoteInContestGossauResult);
    }

    [Fact]
    public async Task WriteEndpointShouldThrowIfNotPublisher()
    {
        if (_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        using var channel = CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin);
        var client = new VoteResultService.VoteResultServiceClient(channel);
        var ex = await Assert.ThrowsAnyAsync<RpcException>(async () => await client
            .DefineEntryAsync(new()
            {
                ResultEntry = VoteResultEntry.FinalResults,
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestGossauResult,
            }));
        ex.StatusCode.Should().Be(StatusCode.Unimplemented);
    }

    [Fact]
    public async Task ReadEndpointShouldWorkIfPublisher()
    {
        if (!_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        DatabaseUtil.Truncate(GetService<DataContext>());
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) => permissionBuilder.RebuildPermissionTree());
        using var channel = CreateGrpcChannel(tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id, roles: RolesMockedData.MonitoringElectionAdmin);
        var client = new ContestService.ContestServiceClient(channel);
        var cc = await client.GetAsync(new() { Id = ContestMockedData.IdStGallenEvoting });
        cc.Id.Should().Be(ContestMockedData.IdStGallenEvoting);
    }

    [Fact]
    public async Task ReadEndpointShouldThrowIfNotPublisher()
    {
        if (_serviceMode.HasFlag(ServiceMode.Publisher))
        {
            return;
        }

        using var channel = CreateGrpcChannel(tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id, roles: RolesMockedData.MonitoringElectionAdmin);
        var client = new ContestService.ContestServiceClient(channel);
        var ex = await Assert.ThrowsAsync<RpcException>(async () => await client.GetAsync(new() { Id = ContestMockedData.IdStGallenEvoting }));
        ex.StatusCode.Should().Be(StatusCode.Unimplemented);
    }

    [Fact]
    public async Task EventProcessingShouldWorkIfEventProcessor()
    {
        if (!_serviceMode.HasFlag(ServiceMode.EventProcessor))
        {
            return;
        }

        var testPublisher = GetService<TestEventPublisher>();

        DatabaseUtil.Truncate(GetService<DataContext>());
        var id = Guid.Parse("7ef5e239-be6f-4d4c-89c9-b3a39cdc41ff");
        await testPublisher.Publish(new CountingCircleCreated
        {
            CountingCircle = new()
            {
                Bfs = "123",
                Code = "123",
                Id = id.ToString(),
                Name = "test",
                ResponsibleAuthority = new(),
                ContactPersonAfterEvent = new(),
                ContactPersonDuringEvent = new(),
                ContactPersonSameDuringEventAsAfter = true,
            },
            EventInfo = new() { Timestamp = MockedClock.UtcNowTimestamp },
        });

        var cc = await GetService<DataContext>().CountingCircles.FirstAsync(cc => cc.Id == id);
        cc.Bfs.Should().Be("123");
    }

    [Fact(Skip = "Metric endpoint test is not working properly with dedicated prometheus metric server port (ref: VOTING-4006)")]
    public async Task MetricsEndpointShouldWork()
    {
        var client = CreateHttpClient(false);
        var response = await client.GetPrometheusMetricsAsync();
        response
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public async Task HealthEndpointShouldWork()
    {
        var client = CreateHttpClient(false);
        await client.GetStringAsync("/healthz");
    }
}
