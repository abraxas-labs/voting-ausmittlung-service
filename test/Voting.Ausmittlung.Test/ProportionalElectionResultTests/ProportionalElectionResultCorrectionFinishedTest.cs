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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultCorrectionFinishedTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultCorrectionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCorrectionFinished>().MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultPublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithoutComment()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(x => x.Comment = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCorrectionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithTooManyBallotsInDeletedBundle()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await SeedBallots(BallotBundleState.Deleted);
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.ReadyForCorrection);
            await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultCorrectionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCorrectionFinished>().MatchSnapshot();
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
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultPublished>().Should().NotBeEmpty();
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
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEndedWithEmptySecondFactorId()
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
    public async Task TestShouldReturnAsErfassungElectionAdminWithBundlesAllDoneOrDeleted()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var bundles = await db.ProportionalElectionBundles.AsTracking().ToListAsync();
            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            bundles[0].State = BallotBundleState.Deleted;
            await db.SaveChangesAsync();
        });
        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAsync(
                new ProportionalElectionResultCorrectionFinishedRequest
                {
                    ElectionResultId = IdNotFound,
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
                new ProportionalElectionResultCorrectionFinishedRequest
                {
                    ElectionResultId = IdBadFormat,
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
        await RunEvents<ProportionalElectionResultCorrectionFinished>();

        await AssertCurrentState(CountingCircleResultState.CorrectionDone);
        await AssertSubmissionDoneTimestamp(true);
        await AssertReadyForCorrectionTimestamp(false);
        var comments = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen)
            .ToListAsync());
        comments.MatchSnapshot(x => x.Id);

        var id = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        await AssertHasPublishedEventProcessedMessage(ProportionalElectionResultCorrectionFinished.Descriptor, id);
    }

    [Fact]
    public async Task TestProcessorWithoutComment()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        await ErfassungElectionAdminClient.CorrectionFinishedAsync(NewValidRequest(req => req.Comment = string.Empty));
        await RunEvents<ProportionalElectionResultCorrectionFinished>();

        await AssertCurrentState(CountingCircleResultState.CorrectionDone);
        var comment = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen)
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
            async () => await StGallenErfassungElectionAdminClient.CorrectionFinishedAsync(new ProportionalElectionResultCorrectionFinishedRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .CorrectionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ProportionalElectionResultCorrectionFinishedRequest NewValidRequest(
        Action<ProportionalElectionResultCorrectionFinishedRequest>? customizer = null)
    {
        var req = new ProportionalElectionResultCorrectionFinishedRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            Comment = "my-comment",
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
        customizer?.Invoke(req);
        return req;
    }

    private async Task AssertSubmissionDoneTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.ProportionalElectionResults.SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen)));
        (result.SubmissionDoneTimestamp != null).Should().Be(hasTimestamp);
    }

    private async Task AssertReadyForCorrectionTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.ProportionalElectionResults.SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen)));
        (result.ReadyForCorrectionTimestamp != null).Should().Be(hasTimestamp);
    }
}
