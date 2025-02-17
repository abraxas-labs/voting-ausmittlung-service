// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultCorrectionFinishedTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultCorrectionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultCorrectionFinished>().MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<VoteResultPublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.ReadyForCorrection);
            await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultCorrectionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithoutComment()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(x => x.Comment = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldAutomaticallyPublishBeforeAuditedTentativelyWithRelatedCantonSettingsAndDoiLevel()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x =>
            {
                x.ManualPublishResultsEnabled = false;
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<VoteResultPublished>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(
                new VoteResultCorrectionFinishedRequest
                {
                    VoteResultId = IdNotFound,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(
                new VoteResultCorrectionFinishedRequest
                {
                    VoteResultId = IdBadFormat,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
                }),
            StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertSubmissionDoneTimestamp(false);
        await AssertReadyForCorrectionTimestamp(true);

        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        await RunEvents<VoteResultCorrectionFinished>();

        await AssertCurrentState(CountingCircleResultState.CorrectionDone);
        await AssertSubmissionDoneTimestamp(true);
        await AssertReadyForCorrectionTimestamp(false);
        var comments = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
            .ToListAsync());
        comments.MatchSnapshot(x => x.Id);

        var id = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.CorrectionDone);
    }

    [Fact]
    public async Task TestProcessorNoComment()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(req => req.Comment = string.Empty));
        await RunEvents<VoteResultCorrectionFinished>();

        await AssertCurrentState(CountingCircleResultState.CorrectionDone);
        var comment = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
            .FirstOrDefaultAsync());
        comment.Should().BeNull();
    }

    [Fact]
    public async Task TestShouldThrowDataChanged()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalTokenJwtIds!.Contains(SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction));

            item.ActionId = "updated-action-id";
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Data changed during the second factor transaction");
    }

    [Fact]
    public async Task TestShouldThrowNotVerified()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        const string invalidExternalId = "a11c61aa-af52-431b-9c0e-f86d24d8a72b";
        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalTokenJwtIds!.Contains(SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction));

            item.ExternalTokenJwtIds = [invalidExternalId];
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(new VoteResultCorrectionFinishedRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await new VoteResultService.VoteResultServiceClient(channel)
            .CorrectionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private VoteResultCorrectionFinishedRequest NewValidRequest(
        Action<VoteResultCorrectionFinishedRequest>? customizer = null)
    {
        var req = new VoteResultCorrectionFinishedRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            Comment = "my-comment",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
        customizer?.Invoke(req);
        return req;
    }

    private async Task AssertSubmissionDoneTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.VoteResults.SingleAsync(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult)));
        (result.SubmissionDoneTimestamp != null).Should().Be(hasTimestamp);
    }

    private async Task AssertReadyForCorrectionTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.VoteResults.SingleAsync(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult)));
        (result.ReadyForCorrectionTimestamp != null).Should().Be(hasTimestamp);
    }
}
