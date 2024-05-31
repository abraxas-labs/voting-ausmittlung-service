// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultFinalizeTest : ProportionalElectionEndResultBaseTest
{
    public ProportionalElectionEndResultFinalizeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();
        await SetAllAuditedTentatively();
    }

    [Fact]
    public async Task ShouldWork()
    {
        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = true);
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.HasOpenRequiredLotDecisions = false);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultFinalized>();
        ev.ProportionalElectionId.Should().Be(ProportionalElectionEndResultMockedData.ElectionId);
        ev.ProportionalElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            // set all lot decisions as done
            await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = true);
            await ModifyDbEntities<ProportionalElectionListEndResult>(
                x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
                x => x.HasOpenRequiredLotDecisions = false);
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionEndResultFinalized>();
        });
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(request.ProportionalElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        // testing phase
        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == electionId,
            x => x.LotDecision = true);
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == electionId,
            x => x.HasOpenRequiredLotDecisions = false);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultFinalized>();
        await RunEvents<ProportionalElectionEndResultFinalized>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.ProportionalElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == electionId,
            x => x.LotDecision = true);
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == electionId,
            x => x.HasOpenRequiredLotDecisions = false);
        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == electionId,
            e => e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultFinalized>();
        await RunEvents<ProportionalElectionEndResultFinalized>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);
        evTestingPhaseEnded.ProportionalElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task ShouldWorkOnlyNonRequiredLotDecisions()
    {
        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.LotDecisionRequired = false);
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.HasOpenRequiredLotDecisions = false);
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultFinalized>();
        ev.ProportionalElectionId.Should().Be(ProportionalElectionEndResultMockedData.ElectionId);
        ev.ProportionalElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task ShouldThrowCountingCirclesNotDone()
    {
        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
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
    public async Task ShouldThrowMissingRequiredManualEndResult()
    {
        var electionId = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);

        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == electionId,
            x => x.LotDecision = true);
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x =>
            {
                x.NumberOfMandates = 0;
                x.HasOpenRequiredLotDecisions = false;
            });
        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == electionId,
            x => x.ManualEndResultRequired = true);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Manual end result is required and not all number of mandates are distributed");
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
        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId)));
        result.Finalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultFinalized
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
        result.Finalized.Should().BeTrue();
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
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeProportionalElectionEndResultRequest
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                SecondFactorTransactionId = invalidExternalId,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    [Fact]
    public async Task ShouldThrowWithDpAlgorithmAndNoDpResult()
    {
        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "finalization is not possible if the double proportional election result is not calculated");
    }

    [Fact]
    public async Task ShouldThrowWithUnfinishedDpResult()
    {
        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiQuorum);

        await RunOnDb(async db =>
        {
            db.DoubleProportionalResults.Add(new()
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionGuid,
            });
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "finalization is only possible if the double proportional election result distributed all number of mandates");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        // set all lot decisions as done
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            x => x.ListEndResult.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.LotDecision = true);
        await ModifyDbEntities<ProportionalElectionListEndResult>(
            x => x.ElectionEndResult.ProportionalElectionId == Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId),
            x => x.HasOpenRequiredLotDecisions = false);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .FinalizeEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private FinalizeProportionalElectionEndResultRequest NewValidRequest()
    {
        return new FinalizeProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };
    }
}
