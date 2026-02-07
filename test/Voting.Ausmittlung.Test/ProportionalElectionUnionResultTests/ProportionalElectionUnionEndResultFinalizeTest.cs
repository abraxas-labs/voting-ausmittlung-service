// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
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
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public class ProportionalElectionUnionEndResultFinalizeTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionEndResultFinalizeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SecondFactorTransactionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionEndResultFinalized>();
        ev.ProportionalElectionUnionId.Should().Be(ZhMockedData.ProportionalElectionUnionIdKtrat);
        ev.ProportionalElectionUnionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ZhMockedData.ContestIdBund, async () =>
        {
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUnionEndResultFinalized>();
        });
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        // testing phase
        var request = NewValidRequest();
        var contestId = ZhMockedData.ContestGuidBund;
        var unionId = Guid.Parse(request.ProportionalElectionUnionId);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionEndResultFinalized>();
        await RunEvents<ProportionalElectionUnionEndResultFinalized>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, false);
        evInTestingPhase.ProportionalElectionUnionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        // set all elections done
        await ModifyDbEntities<ProportionalElectionUnionEndResult>(
            x => x.ProportionalElectionUnionId == unionId,
            x =>
            {
                x.CountOfDoneElections = 3;
                x.TotalCountOfElections = 3;
            });

        await ModifyDbEntities<ProportionalElectionListEndResult>(
            l => l.ElectionEndResult.ProportionalElection.ProportionalElectionUnionEntries.Any(x => x.ProportionalElectionUnionId == unionId),
            l => l.ConventionalSubTotal.UnmodifiedListVotesCount = 2000);

        await MonitoringElectionAdminClient.FinalizeEndResultAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionEndResultFinalized>();
        await RunEvents<ProportionalElectionUnionEndResultFinalized>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionId, true);
        evTestingPhaseEnded.ProportionalElectionUnionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await SetContestState(ZhMockedData.ContestIdBund, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task ShouldThrowElectionsNotDone()
    {
        await ModifyDbEntities<ProportionalElectionUnionEndResult>(
            x => x.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat,
            x => x.CountOfDoneElections--);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Not all elections are done");
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await CreateService("unknown-tenant", roles: RolesMockedData.MonitoringElectionAdmin).FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldThrowWithWrongMandateAlgorithm()
    {
        await ModifyDbEntities<ProportionalElection>(
            x => x.ProportionalElectionUnionEntries.Any(y => y.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat),
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Can only finalize unions with a union double proportional mandate algorithm");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = false,
            splitQuery: true);

        var result = await RunOnDb(x => x.ProportionalElectionUnionEndResults
            .Include(r => r.ProportionalElectionUnion.DoubleProportionalResult)
            .FirstAsync(r => r.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));
        result.Finalized.Should().BeFalse();
        result.ProportionalElectionUnion.DoubleProportionalResult.Should().BeNull();

        var electionGuid = ZhMockedData.ProportionalElectionGuidKtratWinterthur;

        await RunOnDb(async db =>
        {
            var candidateEndResult = await db.ProportionalElectionCandidateEndResult
                .AsTracking()
                .FirstAsync(x => x.ListEndResult.List.ProportionalElectionId == electionGuid);
            candidateEndResult.ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionEndResultFinalized
            {
                ProportionalElectionUnionId = result.ProportionalElectionUnionId.ToString(),
                ProportionalElectionUnionEndResultId = result.Id.ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });

        result = await RunOnDb(x => x.ProportionalElectionUnionEndResults
            .Include(r => r.ProportionalElectionUnion.DoubleProportionalResult)
            .FirstAsync(r => r.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));
        result.Finalized.Should().BeTrue();
        result.ProportionalElectionUnion.DoubleProportionalResult.Should().NotBeNull();

        await AssertHasPublishedEventProcessedMessage(ProportionalElectionUnionEndResultFinalized.Descriptor, result.Id);

        var dpResult = await MonitoringElectionAdminClient.GetDoubleProportionalResultAsync(new() { ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat });
        dpResult.MatchSnapshot("dpResult");

        var endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionGuid));

        var simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(y => y.Id == electionGuid));

        endResult.ListEndResults.Should().NotBeEmpty();
        endResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();

        endResult.ListEndResults.Any(x => x.LotDecisionState is ElectionLotDecisionState.OpenAndRequired).Should().BeFalse();

        var candidateEndResults = endResult.ListEndResults.SelectMany(x => x.CandidateEndResults).ToList();
        candidateEndResults.Any(x => x.Rank == 1 && x.State == ProportionalElectionCandidateEndResultState.Elected)
            .Should()
            .BeTrue();

        candidateEndResults.Any(x => x.LotDecisionEnabled || x.LotDecisionRequired || x.State is ProportionalElectionCandidateEndResultState.Pending)
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task TestProcessorWithDisabledCantonSettingsEndResultFinalize()
    {
        var result = await RunOnDb(x => x.ProportionalElectionUnionEndResults
            .Include(r => r.ProportionalElectionUnion.DoubleProportionalResult)
            .FirstAsync(r => r.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));
        result.Finalized.Should().BeFalse();

        var electionGuid = ZhMockedData.ProportionalElectionGuidKtratWinterthur;

        var simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(y => y.Id == electionGuid));
        simplePb.EndResultFinalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionEndResultFinalized
            {
                ProportionalElectionUnionId = result.ProportionalElectionUnionId.ToString(),
                ProportionalElectionUnionEndResultId = result.Id.ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });

        var endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionGuid));

        endResult.Finalized.Should().BeTrue();

        simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(y => y.Id == electionGuid));
        simplePb.EndResultFinalized.Should().BeTrue();
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
            async () => await MonitoringElectionAdminClient.FinalizeEndResultAsync(new FinalizeProportionalElectionUnionEndResultRequest
            {
                ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
            }),
            StatusCode.FailedPrecondition,
            "Second factor transaction is not verified");
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .FinalizeEndResultAsync(NewValidRequest());
    }

    private FinalizeProportionalElectionUnionEndResultRequest NewValidRequest()
    {
        return new()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.SecondFactorTransactionIdString,
        };
    }
}
