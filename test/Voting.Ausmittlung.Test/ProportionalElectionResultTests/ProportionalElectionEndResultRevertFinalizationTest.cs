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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultRevertFinalizationTest : ProportionalElectionEndResultBaseTest
{
    public ProportionalElectionEndResultRevertFinalizationTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await ModifyDbEntities<ProportionalElectionEndResult>(
            r => r.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            r => r.Finalized = true);
    }

    [Fact]
    public async Task ShouldWork()
    {
        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.HasOpenRequiredLotDecisions = false);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        });

        await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultFinalizationReverted>();
        ev.ProportionalElectionId.Should().Be(ProportionalElectionEndResultMockedData.ElectionId);
        ev.ProportionalElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            // set all lot decisions as done
            await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.HasOpenRequiredLotDecisions = false);
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeProportionalElectionEndResultRequest
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            });

            await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionEndResultFinalizationReverted>();
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
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
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
    public async Task ShouldThrowMandateDistrubtionNotStarted()
    {
        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.MandateDistributionTriggered = false);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot revert finalization if mandate distribution is not triggered yet");
    }

    [Fact]
    public async Task TestProcessor()
    {
        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId)));
        result.Finalized.Should().BeTrue();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultFinalizationReverted
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });

        result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(vr => vr.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId)));
        result.Finalized.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // Finalize it first, so it can be reverted
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.HasOpenRequiredLotDecisions = false);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        });

        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .RevertEndResultFinalizationAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RevertProportionalElectionEndResultFinalizationRequest NewValidRequest()
    {
        return new RevertProportionalElectionEndResultFinalizationRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        };
    }
}
