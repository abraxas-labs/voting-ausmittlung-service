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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionDoubleProportionalResultSuperApportionmentUpdateLotDecisionTest : ProportionalElectionDoubleProportionalResultBaseTest
{
    public ProportionalElectionDoubleProportionalResultSuperApportionmentUpdateLotDecisionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await CreateDpResult(ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot);
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated>();
        ev.MatchSnapshot();
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
        await DeleteDpResult(ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(new()
            {
                ProportionalElectionId = ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot.ToString(),
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
        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = false,
            splitQuery: true);

        var electionId = ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot;
        var svpListId = Guid.Parse("3b083853-cd08-4b46-b81b-da849a979bfc");
        var spListId = Guid.Parse("868cd951-5faf-4799-81ad-1ebd65874e52");

        var dpResult = await RunScoped<DoubleProportionalResultRepo, DoubleProportionalResult>(repo => repo.GetElectionDoubleProportionalResult(electionId)!);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.HasOpenLotDecision);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Unspecified);
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision.Should().Be(1);
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(4);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(0);
        dpResult.NumberOfMandates.Should().Be(5);

        var svpColumn = dpResult.Columns.Single(c => c.ListId == svpListId);
        svpColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(0);
        svpColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(0);
        svpColumn.SuperApportionmentNumberOfMandates.Should().Be(0);

        var spColumn = dpResult.Columns.Single(c => c.ListId == spListId);
        spColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(0);
        spColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(0);
        spColumn.SuperApportionmentNumberOfMandates.Should().Be(0);

        var endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionId));

        var simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(r => r.Id == electionId));

        endResult.ListEndResults.Any(x => x.LotDecisionState is ElectionLotDecisionState.OpenAndRequired).Should().BeFalse();
        endResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated
            {
                ProportionalElectionId = dpResult.ProportionalElectionId.ToString(),
                DoubleProportionalResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(null, electionId, false).ToString(),
                Number = 2,
                Columns =
                {
                    new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData()
                    {
                        ListId = svpListId.ToString(),
                        NumberOfMandates = 0,
                    },
                    new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData()
                    {
                        ListId = spListId.ToString(),
                        NumberOfMandates = 1,
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        dpResult = await RunScoped<DoubleProportionalResultRepo, DoubleProportionalResult>(repo => repo.GetElectionDoubleProportionalResult(electionId)!);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Completed);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Unspecified);
        dpResult.SuperApportionmentNumberOfMandatesForLotDecision.Should().Be(1);
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(5);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(0);
        dpResult.NumberOfMandates.Should().Be(5);

        svpColumn = dpResult.Columns.Single(c => c.ListId == svpListId);
        svpColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(0);
        svpColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(0);
        svpColumn.SuperApportionmentNumberOfMandates.Should().Be(0);

        spColumn = dpResult.Columns.Single(c => c.ListId == spListId);
        spColumn.SuperApportionmentNumberOfMandatesExclLotDecision.Should().Be(0);
        spColumn.SuperApportionmentNumberOfMandatesFromLotDecision.Should().Be(1);
        spColumn.SuperApportionmentNumberOfMandates.Should().Be(1);

        endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionId));

        simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(r => r.Id == electionId));

        endResult.ListEndResults.All(x => x.LotDecisionState is ElectionLotDecisionState.None).Should().BeTrue();
        endResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();
    }

    [Fact]
    public async Task TestProcessorWithDisabledCantonSettingsEndResultFinalize()
    {
        var electionId = ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot;
        var svpListId = Guid.Parse("3b083853-cd08-4b46-b81b-da849a979bfc");
        var spListId = Guid.Parse("868cd951-5faf-4799-81ad-1ebd65874e52");

        var endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .SingleAsync(x => x.ProportionalElectionId == electionId));

        var simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(r => r.Id == electionId));

        endResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated
            {
                ProportionalElectionId = electionId.ToString(),
                DoubleProportionalResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(null, electionId, false).ToString(),
                Number = 2,
                Columns =
                {
                    new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData()
                    {
                        ListId = svpListId.ToString(),
                        NumberOfMandates = 0,
                    },
                    new ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData()
                    {
                        ListId = spListId.ToString(),
                        NumberOfMandates = 1,
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .SingleAsync(x => x.ProportionalElectionId == electionId));

        simplePb = await RunOnDb(x => x.SimplePoliticalBusinesses.SingleAsync(r => r.Id == electionId));

        endResult.Finalized.Should().BeTrue();
        simplePb.EndResultFinalized.Should().BeTrue();
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .UpdateDoubleProportionalResultSuperApportionmentLotDecisionAsync(NewValidRequest());
    }

    private UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest NewValidRequest(
        Action<UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionDoubleProportionalResultSuperApportionmentLotDecisionRequest()
        {
            ProportionalElectionId = ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot.ToString(),
            Number = 2,
        };

        action?.Invoke(request);
        return request;
    }
}
