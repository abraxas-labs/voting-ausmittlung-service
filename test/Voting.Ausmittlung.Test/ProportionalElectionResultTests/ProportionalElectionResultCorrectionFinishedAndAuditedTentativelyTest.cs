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
using PermissionService = Voting.Ausmittlung.Core.Services.Permission.PermissionService;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest());
        var events = new List<IMessage>
        {
            EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCorrectionFinished>(),
            EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultAuditedTentatively>(),
        };
        events.MatchSnapshot();
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultPublished>().ElectionResultId.Should().Be(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.ReadyForCorrection);
            await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest());
            return new[]
            {
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultCorrectionFinished>(),
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultAuditedTentatively>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldThrowForNonSelfOwnedPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);

        var permissionService = GetService<PermissionService>();
        permissionService.SetAbraxasAuthIfNotAuthenticated();
        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(
            ContestMockedData.GuidStGallenEvoting,
            CountingCircleMockedData.GuidGossau,
            true);
        var ccDetails = await AggregateRepositoryMock.GetOrCreateById<ContestCountingCircleDetailsAggregate>(ccDetailsId);
        ccDetails.CreateFrom(
            new ContestCountingCircleDetails
            {
                ContestId = ContestMockedData.GuidStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.GuidGossau,
            },
            ContestMockedData.GuidStGallenEvoting,
            CountingCircleMockedData.GuidGossau,
            true);
        await AggregateRepositoryMock.Save(ccDetails);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithTooManyBallotsInDeletedBundle()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await SeedBallots(BallotBundleState.Deleted);
        await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest());
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
        await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(
                new ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest
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
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(
                new ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowWithEmptySecondFactorId()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty)),
            StatusCode.InvalidArgument);
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
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
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
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(new ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowForNonCommunalPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);

        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.Gossau.Id && x.SnapshotContestId == ContestMockedData.GuidStGallenEvoting,
            x => x.Type = DomainOfInfluenceType.Ct);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "finish correction and audit tentatively is not allowed for non communal political business");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .CorrectionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest>? customizer = null)
    {
        var req = new ProportionalElectionResultCorrectionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
        customizer?.Invoke(req);
        return req;
    }
}
