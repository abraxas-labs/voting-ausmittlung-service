// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultSubmissionFinishedTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionFinished>().MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<VoteResultPublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await StGallenErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldAutomaticallyPublishBeforeAuditedTentativelyWithRelatedCantonSettingsAndDoiLevel()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x =>
            {
                x.ManualPublishResultsEnabled = false;
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<VoteResultPublished>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionOngoing);
            await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await StGallenErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(
                new VoteResultSubmissionFinishedRequest
                {
                    VoteResultId = IdNotFound,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(
                new VoteResultSubmissionFinishedRequest
                {
                    VoteResultId = IdBadFormat,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
                }),
            StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertSubmissionDoneTimestamp(false);

        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
        await RunEvents<VoteResultSubmissionFinished>();

        await AssertCurrentState(CountingCircleResultState.SubmissionDone);
        await AssertSubmissionDoneTimestamp(true);

        var id = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
        await AssertHasPublishedEventProcessedMessage(VoteResultSubmissionFinished.Descriptor, id);
    }

    [Fact]
    public async Task TestShouldThrowDataChanged()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalTokenJwtIds!.Contains(SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction));

            item.ActionId = "updated-action-id";
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Data changed during the second factor transaction");
    }

    [Fact]
    public async Task TestShouldThrowNotVerified()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);

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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(new VoteResultSubmissionFinishedRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await new VoteResultService.VoteResultServiceClient(channel)
            .SubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private VoteResultSubmissionFinishedRequest NewValidRequest(Action<VoteResultSubmissionFinishedRequest>? customizer = null)
    {
        var req = new VoteResultSubmissionFinishedRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
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
}
