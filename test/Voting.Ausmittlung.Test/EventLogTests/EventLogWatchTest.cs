// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.EventLogTests;

public class EventLogWatchTest : BaseTest<EventLogService.EventLogServiceClient>
{
    public EventLogWatchTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) => permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public void EventFilterPoliticalBusinessResultTest()
    {
        var politicalBusinessResultId = Guid.NewGuid();
        var filter = new EventLogReader.EventFilter("364d0bf2-e689-4b38-9e75-a5e2f230d952", new HashSet<string> { ResultImportCompleted.Descriptor.FullName }, null, politicalBusinessResultId);

        // matching
        filter.Filter(
                new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now) { PoliticalBusinessResultId = politicalBusinessResultId })
            .Should()
            .BeTrue();

        // other event type
        filter.Filter(
                new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now) { PoliticalBusinessResultId = politicalBusinessResultId })
            .Should()
            .BeTrue();

        // missing political business result id
        filter.Filter(new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now))
            .Should()
            .BeFalse();

        // mismatched political business result id
        filter.Filter(
                new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now) { PoliticalBusinessResultId = Guid.NewGuid() })
            .Should()
            .BeFalse();
    }

    [Fact]
    public void EventFilterPoliticalBusinessTest()
    {
        var politicalBusinessId = Guid.NewGuid();
        var filter = new EventLogReader.EventFilter("364d0bf2-e689-4b38-9e75-a5e2f230d952", new HashSet<string> { ResultImportCompleted.Descriptor.FullName }, politicalBusinessId, null);

        // matching
        filter.Filter(
                new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now)
                {
                    PoliticalBusinessId = politicalBusinessId,
                })
            .Should()
            .BeTrue();

        // other event type
        filter.Filter(
                new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now)
                {
                    PoliticalBusinessId = politicalBusinessId,
                })
            .Should()
            .BeTrue();

        // missing political business id
        filter.Filter(new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now))
            .Should()
            .BeFalse();

        // mismatched political business id
        filter.Filter(
                new EventProcessedMessage(ResultImportCompleted.Descriptor.FullName, DateTime.Now)
                {
                    PoliticalBusinessId = Guid.NewGuid(),
                })
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task ShouldProcess()
    {
        var events = await PublishAndWatchEvents(
            [
                new WatchEventsRequestFilter
                {
                    Id = "d3860401-e406-4a13-be88-8b0cbfa23095",
                    PoliticalBusinessResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenResult,
                    Types_ = { VoteResultAuditedTentatively.Descriptor.FullName, },
                },
            ],
            [
                new VoteResultAuditedTentatively
                {
                    EventInfo = GetMockedEventInfo(),
                    VoteResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenResult,
                },
            ],
            1);
        events.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new EventLogService.EventLogServiceClient(channel).Watch(
            new WatchEventsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdStGallen,
            },
            new(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    private async Task<List<Event>> PublishAndWatchEvents<TEvent>(
        IEnumerable<WatchEventsRequestFilter> filters,
        IEnumerable<TEvent> events,
        int expectedReceivedCount)
        where TEvent : IMessage<TEvent>
    {
        var request = new WatchEventsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
            Filters = { filters },
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var responseStream = ErfassungElectionAdminClient.Watch(
            request,
            new(cancellationToken: cts.Token));

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);

        await TestEventPublisher.Publish(events.ToArray());
        return await responseStream.ResponseStream.ReadNIgnoreCancellation(expectedReceivedCount, cts.Token);
    }
}
