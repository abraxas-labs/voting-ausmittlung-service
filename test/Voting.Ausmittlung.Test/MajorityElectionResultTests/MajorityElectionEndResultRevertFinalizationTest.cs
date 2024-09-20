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

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionEndResultRevertFinalizationTest : MajorityElectionEndResultBaseTest
{
    public MajorityElectionEndResultRevertFinalizationTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishResultSubmissions();
        await SetResultsToAuditedTentatively();
        await ModifyDbEntities<MajorityElectionEndResult>(
            r => r.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            r => r.Finalized = true);
    }

    [Fact]
    public async Task ShouldWork()
    {
        // set all lot decisions as done
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        });

        await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultFinalizationReverted>();
        ev.MajorityElectionId.Should().Be(MajorityElectionEndResultMockedData.ElectionId);
        ev.MajorityElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            // set all lot decisions as done
            await ModifyDbEntities<MajorityElectionCandidateEndResult>(
                x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
                x => x.LotDecisionRequired = false);
            await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
                x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
                x => x.LotDecisionRequired = false);
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeMajorityElectionEndResultRequest
            {
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });

            await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionEndResultFinalizationReverted>();
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
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
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
        var result = await RunOnDb(x => x.MajorityElectionEndResults.FirstAsync(r => r.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId)));
        result.Finalized.Should().BeTrue();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionEndResultFinalizationReverted
            {
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                MajorityElectionEndResultId = result.Id.ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });

        result = await RunOnDb(x => x.MajorityElectionEndResults.FirstAsync(vr => vr.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId)));
        result.Finalized.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // set all lot decisions as done
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);

        // Finalize it first, so it can be reverted
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        });

        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .RevertEndResultFinalizationAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RevertMajorityElectionEndResultFinalizationRequest NewValidRequest()
    {
        return new RevertMajorityElectionEndResultFinalizationRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
        };
    }
}
