// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultAuditedTentativelyTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultAuditedTentatively>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.VoteResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.VoteResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.VoteResultIds.Add(VoteResultMockedData.IdGossauVoteInContestStGallenResult))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        // mock some eVoting data to test it is attached correctly to the end result
        await ModifyDbEntities(
            (BallotQuestionResult r) => r.QuestionId == Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
            r =>
            {
                r.EVotingSubTotal.TotalCountOfAnswerYes = 10;
                r.EVotingSubTotal.TotalCountOfAnswerNo = 20;
                r.EVotingSubTotal.TotalCountOfAnswerUnspecified = 5;
            });
        await ModifyDbEntities(
            (TieBreakQuestionResult r) => r.QuestionId == Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
            r =>
            {
                r.EVotingSubTotal.TotalCountOfAnswerQ1 = 11;
                r.EVotingSubTotal.TotalCountOfAnswerQ2 = 21;
                r.EVotingSubTotal.TotalCountOfAnswerUnspecified = 6;
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultAuditedTentatively
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
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new VoteResultService.VoteResultServiceClient(channel)
            .AuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private VoteResultAuditedTentativelyRequest NewValidRequest(Action<VoteResultAuditedTentativelyRequest>? customizer = null)
    {
        var r = new VoteResultAuditedTentativelyRequest
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
