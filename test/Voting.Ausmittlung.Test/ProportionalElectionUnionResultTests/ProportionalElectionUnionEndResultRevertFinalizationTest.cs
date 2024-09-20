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
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public class ProportionalElectionUnionEndResultRevertFinalizationTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionEndResultRevertFinalizationTest(TestApplicationFactory factory)
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
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        });

        await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionEndResultFinalizationReverted>();
        ev.ProportionalElectionUnionId.Should().Be(ZhMockedData.ProportionalElectionUnionIdKtrat);
        ev.ProportionalElectionUnionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ZhMockedData.ContestIdBund, async () =>
        {
            await MonitoringElectionAdminClient.FinalizeEndResultAsync(new()
            {
                ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });

            await MonitoringElectionAdminClient.RevertEndResultFinalizationAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUnionEndResultFinalizationReverted>();
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
        await SetContestState(ZhMockedData.ContestIdBund, ContestState.PastLocked);
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
    public async Task TestProcessor()
    {
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        });

        await RunEvents<ProportionalElectionUnionEndResultFinalized>();

        var unionGuid = ZhMockedData.ProportionalElectionUnionGuidKtrat;
        var electionGuid = ZhMockedData.ProportionalElectionGuidKtratDietikon;

        // test whether the double proportional result exists and election end results got their update from the double proportional result
        (await RunOnDb(db => db.DoubleProportionalResults.AnyAsync(x => x.ProportionalElectionUnionId == unionGuid)))
            .Should().BeTrue();

        (await RunOnDb(db => db.ProportionalElectionListEndResult.AnyAsync(x =>
            x.ElectionEndResult.ProportionalElectionId == electionGuid && x.NumberOfMandates != 0)))
            .Should().BeTrue();

        // do some random end result db modifications
        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.ListEndResults)
                    .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == electionGuid);

            endResult.Finalized = true;
            endResult.ListEndResults.First().HasOpenRequiredLotDecisions = true;

            var candidateEndResult = endResult.ListEndResults.First().CandidateEndResults.First();
            candidateEndResult.Rank = 2;
            candidateEndResult.State = ProportionalElectionCandidateEndResultState.Elected;
            candidateEndResult.LotDecisionEnabled = true;
            candidateEndResult.LotDecisionRequired = true;
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionEndResultFinalizationReverted
            {
                ProportionalElectionUnionId = unionGuid.ToString(),
                ProportionalElectionUnionEndResultId = AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(unionGuid, false).ToString(),
                EventInfo = new EventInfo
                {
                    Timestamp = new DateTime(2020, 01, 10, 10, 10, 0, DateTimeKind.Utc).ToTimestamp(),
                    User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
                    Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
                },
            });

        var result = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .AsSplitQuery()
            .SingleAsync(y => y.ProportionalElectionUnionId == unionGuid));
        result.Finalized.Should().BeFalse();

        // test that the double proportional result get removed correctly
        var union = await RunOnDb(db => db.ProportionalElectionUnions
            .AsSplitQuery()
            .Include(x => x.EndResult)
            .Include(x => x.DoubleProportionalResult)
            .Include(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection.DoubleProportionalResultRows)
            .Include(x => x.ProportionalElectionUnionLists)
                .ThenInclude(x => x.DoubleProportionalResultColumn)
            .Include(x => x.ProportionalElectionUnionLists)
                .ThenInclude(x => x.ProportionalElectionUnionListEntries)
                    .ThenInclude(x => x.ProportionalElectionList.DoubleProportionalResultCells)
            .Include(x => x.ProportionalElectionUnionEntries)
                .ThenInclude(x => x.ProportionalElection.ProportionalElectionLists)
                    .ThenInclude(x => x.DoubleProportionalResultCells)
            .SingleAsync(x => x.Id == ZhMockedData.ProportionalElectionUnionGuidKtrat));

        union.DoubleProportionalResult.Should().BeNull();
        union.ProportionalElectionUnionEntries.Should().NotBeEmpty();
        union.ProportionalElectionUnionEntries.SelectMany(x => x.ProportionalElection.DoubleProportionalResultRows)
            .WhereNotNull()
            .Should()
            .BeEmpty();
        union.ProportionalElectionUnionEntries.SelectMany(x => x.ProportionalElection!.ProportionalElectionLists.SelectMany(y => y.DoubleProportionalResultCells))
            .WhereNotNull()
            .Should()
            .BeEmpty();
        union.ProportionalElectionUnionLists.SelectMany(x => x.ProportionalElectionUnionListEntries.SelectMany(y => y.ProportionalElectionList.DoubleProportionalResultCells))
            .WhereNotNull()
            .Should()
            .BeEmpty();
        union.ProportionalElectionUnionLists.Select(x => x.DoubleProportionalResultColumn)
            .WhereNotNull()
            .Should()
            .BeEmpty();

        // test that the election end results get resetted correctly
        var endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
                .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionGuid));

        endResult.Finalized.Should().BeFalse();
        endResult.ListEndResults.Should().NotBeEmpty();
        endResult.ListEndResults.All(x => x.NumberOfMandates == 0 && !x.HasOpenRequiredLotDecisions).Should().BeTrue();
        endResult.ListEndResults.SelectMany(x => x.CandidateEndResults).Should().NotBeEmpty();
        endResult.ListEndResults.SelectMany(x => x.CandidateEndResults).All(x => x.Rank == 0 && !x.LotDecisionRequired && !x.LotDecisionEnabled)
            .Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await MonitoringElectionAdminClient.FinalizeEndResultAsync(new()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        });

        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .RevertEndResultFinalizationAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RevertProportionalElectionUnionEndResultFinalizationRequest NewValidRequest()
    {
        return new()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
        };
    }
}
