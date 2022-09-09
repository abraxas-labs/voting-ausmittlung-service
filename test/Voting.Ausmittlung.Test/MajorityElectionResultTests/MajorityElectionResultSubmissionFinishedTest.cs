// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
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

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultSubmissionFinishedTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionFinished>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithTooManyBallotsInDeletedBundle()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SeedBallots(BallotBundleState.Deleted);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionOngoing);
            await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithBundlesAllDoneOrDeleted()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var bundles = await db.MajorityElectionResultBundles.AsTracking().ToListAsync();
            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            bundles[0].State = BallotBundleState.Deleted;
            await db.SaveChangesAsync();
        });
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(
                new MajorityElectionResultSubmissionFinishedRequest
                {
                    ElectionResultId = IdNotFound,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(
                new MajorityElectionResultSubmissionFinishedRequest
                {
                    ElectionResultId = IdBadFormat,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
                }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(
                new MajorityElectionResultSubmissionFinishedRequest
                {
                    ElectionResultId = MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
                }),
            StatusCode.PermissionDenied);
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
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultCorrectionFinished
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                EventInfo = GetMockedEventInfo(),
            });
        await AssertCurrentState(CountingCircleResultState.CorrectionDone);

        var id = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        await AssertHasPublishedMessage<ResultStateChanged>(x =>
            x.Id == id
            && x.NewState == CountingCircleResultState.CorrectionDone);
    }

    [Fact]
    public async Task TestShouldThrowDataChanged()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalIdentifier == SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction);

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
                .FirstAsync(x => x.ExternalIdentifier == SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction);

            item.ExternalIdentifier = invalidExternalId;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(new MajorityElectionResultSubmissionFinishedRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                SecondFactorTransactionId = invalidExternalId,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .SubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private MajorityElectionResultSubmissionFinishedRequest NewValidRequest()
    {
        return new MajorityElectionResultSubmissionFinishedRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };
    }
}
