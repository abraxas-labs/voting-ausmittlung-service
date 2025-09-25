// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ContestCountingCircleDetails = Voting.Ausmittlung.Core.Domain.ContestCountingCircleDetails;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultSubmissionFinishedAndAuditedTentativelyTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultSubmissionFinishedAndAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id && x.SnapshotContestId == ContestMockedData.GuidBundesurnengang,
            x => x.Type = DomainOfInfluenceType.Mu);

        // Make sure that St. Gallen is not the owner of the canton, as that would make the endpoints not require 2FA
        // This never happens from a business point of view
        await ModifyDbEntities<CantonSettings>(
            x => x.Canton == DomainOfInfluenceCanton.Sg,
            x => x.SecureConnectId = "someone-else");
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
        var events = new List<IMessage>
        {
            EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionFinished>(),
            EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultAuditedTentatively>(),
        };
        events.MatchSnapshot();
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultPublished>().ElectionResultId.Should().Be(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithTooManyBallotsInDeletedBundle()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SeedBallots(BallotBundleState.Deleted);
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionOngoing);
            await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
            return new[]
            {
                EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultSubmissionFinished>(),
                EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultAuditedTentatively>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldThrowForNonSelfOwnedPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
                }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);

        var permissionService = GetService<Core.Services.Permission.PermissionService>();
        permissionService.SetAbraxasAuthIfNotAuthenticated();
        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(
            ContestMockedData.GuidBundesurnengang,
            CountingCircleMockedData.GuidStGallen,
            true);
        var ccDetails = await AggregateRepositoryMock.GetOrCreateById<ContestCountingCircleDetailsAggregate>(ccDetailsId);
        ccDetails.CreateFrom(
            new ContestCountingCircleDetails
            {
                ContestId = ContestMockedData.GuidBundesurnengang,
                CountingCircleId = CountingCircleMockedData.GuidStGallen,
            },
            ContestMockedData.GuidBundesurnengang,
            CountingCircleMockedData.GuidStGallen,
            true);
        await AggregateRepositoryMock.Save(ccDetails);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
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
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdNotFound,
                    SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdBadFormat,
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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowForNonCommunalPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id && x.SnapshotContestId == ContestMockedData.GuidBundesurnengang,
            x => x.Type = DomainOfInfluenceType.Ct);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "finish submission and audit tentatively is not allowed for non communal political business");
    }

    [Fact]
    public async Task TestShouldThrowWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty)),
            StatusCode.InvalidArgument);
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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest>? customizer = null)
    {
        var req = new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
        customizer?.Invoke(req);
        return req;
    }
}
