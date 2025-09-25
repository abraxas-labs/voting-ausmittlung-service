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

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultSubmissionFinishedAndAuditedTentativelyTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultSubmissionFinishedAndAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
        var events = new List<IMessage>
        {
            EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionFinished>(),
            EventPublisherMock.GetSinglePublishedEvent<VoteResultAuditedTentatively>(),
        };
        events.MatchSnapshot();
        EventPublisherMock.GetSinglePublishedEvent<VoteResultPublished>().VoteResultId.Should().Be(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionOngoing);
            await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
            return new[]
            {
                EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultSubmissionFinished>(),
                EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultAuditedTentatively>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldThrowForNonSelfOwnedPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);

        var permissionService = GetService<Core.Services.Permission.PermissionService>();
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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new VoteResultSubmissionFinishedAndAuditedTentativelyRequest
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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new VoteResultSubmissionFinishedAndAuditedTentativelyRequest
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
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
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
        await new VoteResultService.VoteResultServiceClient(channel)
            .SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private VoteResultSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<VoteResultSubmissionFinishedAndAuditedTentativelyRequest>? customizer = null)
    {
        var req = new VoteResultSubmissionFinishedAndAuditedTentativelyRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
        customizer?.Invoke(req);
        return req;
    }
}
