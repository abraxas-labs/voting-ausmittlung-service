// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionEndResultFinalizeTest : MajorityElectionEndResultBaseTest
{
    public MajorityElectionEndResultFinalizeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishResultSubmissions();
        await SetResultsToAuditedTentatively();
    }

    [Fact]
    public async Task ShouldWork()
    {
        // set all lot decisions as done
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = true);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = true);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultFinalized>();
        ev.MajorityElectionId.Should().Be(MajorityElectionEndResultMockedData.ElectionId);
        ev.MajorityElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldWorkOnlyNonRequiredLotDecisions()
    {
        // set all lot decisions as done
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultFinalized>();
        ev.MajorityElectionId.Should().Be(MajorityElectionEndResultMockedData.ElectionId);
        ev.MajorityElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(request.MajorityElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        // testing phase
        // set all lot decisions as done
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultFinalized>();
        await RunEvents<MajorityElectionEndResultFinalized>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.MajorityElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        // set all lot decisions as done
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<MajorityElectionEndResult>(
            e => e.MajorityElectionId == electionId,
            e => e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionEndResultFinalized>();
        await RunEvents<MajorityElectionEndResultFinalized>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);
        evTestingPhaseEnded.MajorityElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task ShouldThrowCountingCirclesNotDone()
    {
        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.CountOfDoneCountingCircles--);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "not all counting circles are done");
    }

    [Fact]
    public async Task ShouldThrowOpenLotDecisions()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "finalization is only possible after all required lot decisions are saved");
    }

    [Fact]
    public async Task ShouldThrowOpenSecondaryLotDecisions()
    {
        await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = false);
        await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
            x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x =>
            {
                x.LotDecisionRequired = true;
                x.LotDecision = false;
            });
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "finalization is only possible after all required lot decisions are saved");
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
        var result = await RunOnDb(x => x.MajorityElectionEndResults.FirstAsync(r => r.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId)));
        result.Finalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionEndResultFinalized
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
        result.Finalized.Should().BeTrue();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            // set all lot decisions as done
            await ModifyDbEntities<MajorityElectionCandidateEndResult>(
            x => x.MajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = true);
            await ModifyDbEntities<SecondaryMajorityElectionCandidateEndResult>(
                x => x.SecondaryMajorityElectionEndResult.PrimaryMajorityElectionEndResult.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
                x => x.LotDecision = true);
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionEndResultFinalized>();
        });
    }

    [Fact]
    public async Task TestShouldThrowDataChanged()
    {
        await RunScoped<TemporaryDataContext>(async db =>
        {
            var item = await db.SecondFactorTransactions
                .AsTracking()
                .FirstAsync(x => x.ExternalIdentifier == SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction);

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
                .FirstAsync(x => x.ExternalIdentifier == SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction);

            item.ExternalIdentifier = invalidExternalId;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeMajorityElectionEndResultRequest
            {
                MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
                SecondFactorTransactionId = invalidExternalId,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .FinalizeEndResultAsync(NewValidRequest());
    }

    private FinalizeMajorityElectionEndResultRequest NewValidRequest()
    {
        return new FinalizeMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionEndResultMockedData.ElectionId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };
    }
}
