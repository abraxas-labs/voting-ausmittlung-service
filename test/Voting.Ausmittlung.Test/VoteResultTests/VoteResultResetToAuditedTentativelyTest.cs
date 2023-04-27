// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultResetToAuditedTentativelyTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultResetToAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultResettedToAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.Plausibilised);
            await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultResettedToAuditedTentatively>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(
                NewValidRequest(x => x.VoteResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(
                NewValidRequest(x => x.VoteResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(
                NewValidRequest(x => x.VoteResultIds.Add(VoteResultMockedData.IdGossauVoteInContestStGallenResult))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultResettedToAuditedTentatively
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.AuditedTentatively);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetVoteEndResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
        });

        endResult.MatchSnapshot();

        var id = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.AuditedTentatively);

        var resultEntity = await RunOnDb(db => db.VoteResults.SingleAsync(x => x.Id == id));
        resultEntity.PlausibilisedTimestamp.Should().BeNull();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.Plausibilised);
        await new VoteResultService.VoteResultServiceClient(channel)
            .ResetToAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private VoteResultsResetToAuditedTentativelyRequest NewValidRequest(Action<VoteResultsResetToAuditedTentativelyRequest>? customizer = null)
    {
        var r = new VoteResultsResetToAuditedTentativelyRequest
        {
            VoteResultIds =
                {
                    VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
