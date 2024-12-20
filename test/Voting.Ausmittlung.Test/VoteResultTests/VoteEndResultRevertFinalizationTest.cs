// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteEndResultRevertFinalizationTest : VoteResultBaseTest
{
    public VoteEndResultRevertFinalizationTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await ModifyDbEntities<VoteEndResult>(
            vr => vr.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
            vr => vr.Finalized = true);
    }

    [Fact]
    public async Task ShouldWork()
    {
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeVoteEndResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        });
        await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<VoteEndResultFinalizationReverted>();
        ev.VoteId.Should().Be(VoteMockedData.IdGossauVoteInContestStGallen);
        ev.VoteEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeVoteEndResultRequest
            {
                VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            });
            await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteEndResultFinalizationReverted>();
        });
    }

    [Fact]
    public Task ShouldThrowIfNotYetFinalized()
    {
        // If the aggregate does not exist at all -> NotFound should be thrown
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest()),
            StatusCode.FailedPrecondition);
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await CreateService("unknown-tenant", roles: RolesMockedData.MonitoringElectionAdmin).RevertEndResultFinalizationAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldThrowCantonSettingsEndResultFinalizeDisabled()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = true,
            splitQuery: true);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "End result finalize is not enabled for contest");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var result = await RunOnDb(x => x.VoteEndResults.FirstAsync(vr => vr.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)));
        result.Finalized.Should().BeTrue();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteEndResultFinalizationReverted
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
        result.Finalized.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // Finalize it first, so it can be reverted
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeVoteEndResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        });

        await new VoteResultService.VoteResultServiceClient(channel)
            .RevertEndResultFinalizationAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RevertVoteEndResultFinalizationRequest NewValidRequest()
    {
        return new RevertVoteEndResultFinalizationRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
        };
    }
}
