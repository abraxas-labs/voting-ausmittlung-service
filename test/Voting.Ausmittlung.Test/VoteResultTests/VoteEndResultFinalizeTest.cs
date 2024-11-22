// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteEndResultFinalizeTest : VoteResultBaseTest
{
    public VoteEndResultFinalizeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.AuditedTentatively);
    }

    [Fact]
    public async Task ShouldWork()
    {
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<VoteEndResultFinalized>();
        ev.VoteId.Should().Be(VoteMockedData.IdGossauVoteInContestStGallen);
        ev.VoteEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteEndResultFinalized>();
        });
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var voteId = Guid.Parse(request.VoteId);
        var contestId = Guid.Parse(ContestMockedData.IdStGallenEvoting);

        // testing phase
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<VoteEndResultFinalized>();
        await RunEvents<VoteEndResultFinalized>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(voteId, false);
        evInTestingPhase.VoteEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        await ModifyDbEntities<VoteEndResult>(
            e => e.VoteId == voteId,
            e => e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<VoteEndResultFinalized>();
        await RunEvents<VoteEndResultFinalized>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(voteId, true);
        evTestingPhaseEnded.VoteEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task ShouldThrowCountingCirclesNotDone()
    {
        await ModifyDbEntities<VoteEndResult>(
            vr => vr.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
            vr => vr.CountOfDoneCountingCircles--);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "not all counting circles are done");
    }

    [Fact]
    public async Task ShouldThrowCantonSettingsEndResultFinalizeDisabled()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = true,
            splitQuery: true);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "End result finalize is not enabled for contest");
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await CreateService("unknown-tenant", roles: RolesMockedData.MonitoringElectionAdmin).FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var result = await RunOnDb(x => x.VoteEndResults.FirstAsync(vr => vr.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)));
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteEndResultFinalized
            {
                VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
                VoteEndResultId = result.Id.ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });

        result = await RunOnDb(x => x.VoteEndResults.FirstAsync(vr => vr.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)));
        result.Finalized.Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldThrowDataChanged()
    {
        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalTokenJwtIds!.Contains(SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction));

            item.ActionId = "updated-action-id";
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Data changed during the second factor transaction");
    }

    [Fact]
    public async Task TestShouldThrowNotVerified()
    {
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
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeVoteEndResultRequest
            {
                VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .FinalizeEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private FinalizeVoteEndResultRequest NewValidRequest()
    {
        return new FinalizeVoteEndResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
    }
}
