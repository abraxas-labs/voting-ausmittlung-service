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
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public class ProportionalElectionUnionDoubleProportionalResultSuperApportionmentUpdateLotDecisionTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionDoubleProportionalResultSuperApportionmentUpdateLotDecisionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidSuperLot);
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated>();
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task NoRequiredLotDecisionsShouldThrow()
    {
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidKtrat);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(new()
            {
                ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
                Number = 1,
            }),
            StatusCode.InvalidArgument,
            "No lots available");
    }

    [Fact]
    public async Task InvalidLotNumberShouldThrow()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(
                NewValidRequest(x => x.Number = 3)),
            StatusCode.InvalidArgument,
            "Lot with number 3 does not exist");
    }

    [Fact]
    public async Task NotFinalizedShouldThrow()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(new()
            {
                ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat,
                Number = 1,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowForeignMonitoringElection()
    {
        await AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var unionId = ZhMockedData.ProportionalElectionUnionGuidSuperLot;
        var svpUnionListId = Guid.Parse("d7b3a8fc-7550-5289-a6ca-c8bff5712bf5");
        var spUnionListId = Guid.Parse("5b94e66e-162b-523c-b4eb-e2fcda8f6d23");

        var dpResult = await RunScoped<DoubleProportionalResultRepo, DoubleProportionalResult>(repo => repo.GetUnionDoubleProportionalResult(unionId)!);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.HasOpenLotDecision);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Initial);
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision.Should().Be(1);
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(4);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(0);
        dpResult.NumberOfMandates.Should().Be(5);

        var svpColumn = dpResult.Columns.Single(c => c.UnionListId == svpUnionListId);
        svpColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(3);
        svpColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(0);
        svpColumn.SuperApportionmentNumberOfMandates.Should().Be(3);

        var spColumn = dpResult.Columns.Single(c => c.UnionListId == spUnionListId);
        spColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(0);
        spColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(0);
        spColumn.SuperApportionmentNumberOfMandates.Should().Be(0);

        var electionEndResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == ZhMockedData.ProportionalElectionGuidSuperLotDietikon));
        electionEndResult.ListEndResults.Any(x => x.HasOpenRequiredLotDecisions).Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated
            {
                ProportionalElectionUnionId = dpResult.ProportionalElectionUnionId.ToString(),
                DoubleProportionalResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(unionId, null, false).ToString(),
                Number = 2,
                Columns =
                {
                    new ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionColumnEventData()
                    {
                        UnionListId = svpUnionListId.ToString(),
                        NumberOfMandates = 3,
                    },
                    new ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionColumnEventData()
                    {
                        UnionListId = spUnionListId.ToString(),
                        NumberOfMandates = 1,
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        dpResult = await RunScoped<DoubleProportionalResultRepo, DoubleProportionalResult>(repo => repo.GetUnionDoubleProportionalResult(unionId)!);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Completed);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Completed);
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision.Should().Be(1);
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(5);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(5);
        dpResult.NumberOfMandates.Should().Be(5);

        svpColumn = dpResult.Columns.Single(c => c.UnionListId == svpUnionListId);
        svpColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(3);
        svpColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(0);
        svpColumn.SuperApportionmentNumberOfMandates.Should().Be(3);

        spColumn = dpResult.Columns.Single(c => c.UnionListId == spUnionListId);
        spColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(0);
        spColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(1);
        spColumn.SuperApportionmentNumberOfMandates.Should().Be(1);

        electionEndResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == ZhMockedData.ProportionalElectionGuidSuperLotDietikon));
        electionEndResult.ListEndResults.Any(x => x.HasOpenRequiredLotDecisions).Should().BeTrue();
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(NewValidRequest());
    }

    private UpdateProportionalElectionUnionDoubleProportionalResultSuperApportionmentLotDecisionRequest NewValidRequest(
        Action<UpdateProportionalElectionUnionDoubleProportionalResultSuperApportionmentLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionUnionDoubleProportionalResultSuperApportionmentLotDecisionRequest()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdSuperLot,
            Number = 2,
        };

        action?.Invoke(request);
        return request;
    }
}
