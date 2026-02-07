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

public class ProportionalElectionUnionDoubleProportionalResultSubApportionmentUpdateLotDecisionTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionDoubleProportionalResultSubApportionmentUpdateLotDecisionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidSubLot);
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSubApportionmentLotDecisionAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated>();
        ev.MatchSnapshot();
    }

    [Fact]
    public async Task NoRequiredLotDecisionsShouldThrow()
    {
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidKtrat);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSubApportionmentLotDecisionAsync(new()
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
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSubApportionmentLotDecisionAsync(
                NewValidRequest(x => x.Number = 3)),
            StatusCode.InvalidArgument,
            "Lot with number 3 does not exist");
    }

    [Fact]
    public async Task NotFinalizedShouldThrow()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateDoubleProportionalResultSubApportionmentLotDecisionAsync(new()
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
            async () => await StGallenMonitoringElectionAdminClient.UpdateDoubleProportionalResultSubApportionmentLotDecisionAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = false,
            splitQuery: true);

        var unionId = ZhMockedData.ProportionalElectionUnionGuidSubLot;
        var electionId = ZhMockedData.ProportionalElectionGuidSubLotDietikon;

        var spUnionListId = Guid.Parse("304d6159-4a7d-5255-bd39-4e37d7ddaf0f");
        var fdpUnionListId = Guid.Parse("c9aa5f87-de0d-5e87-8cbd-602b267e5d33");

        var spMeilenListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotMeilen][1].Item1;
        var spWinterthurListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotWinterthur][1].Item1;
        var fdpMeilenListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotMeilen][2].Item1;
        var fdpWinterthurListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotWinterthur][2].Item1;

        var dpResult = await RunScoped<DoubleProportionalResultRepo, DoubleProportionalResult>(repo => repo.GetUnionDoubleProportionalResult(unionId)!);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Completed);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.HasOpenLotDecision);
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(60);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(58);
        dpResult.NumberOfMandates.Should().Be(60);

        var electionEndResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionId));
        var simplePb = await RunOnDb(db => db.SimplePoliticalBusinesses
            .SingleAsync(x => x.Id == electionId));

        electionEndResult.ListEndResults.Any(x => x.LotDecisionState is ElectionLotDecisionState.OpenAndRequired).Should().BeFalse();
        electionEndResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();

        var subApportionmentNumberOfMandatesCells = dpResult.Columns
            .SelectMany(co => co.Cells)
            .Where(ce
                => ce.SubApportionmentLotDecisionRequired
                && (ce.ListId == spMeilenListId
                || ce.ListId == spWinterthurListId
                || ce.ListId == fdpMeilenListId
                || ce.ListId == fdpWinterthurListId))
            .Select(ce => new[] { ce.SubApportionmentNumberOfMandates, ce.SubApportionmentNumberOfMandatesExclLotDecision, ce.SubApportionmentNumberOfMandatesFromLotDecision })
            .ToList();

        subApportionmentNumberOfMandatesCells.MatchSnapshot("cells-number-of-mandates-before-lot-decision");

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated
            {
                ProportionalElectionUnionId = dpResult.ProportionalElectionUnionId.ToString(),
                DoubleProportionalResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(unionId, null, false).ToString(),
                Number = 2,
                Columns =
                {
                    new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionColumnEventData()
                    {
                        UnionListId = spUnionListId.ToString(),
                        Cells =
                        {
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = spMeilenListId.ToString(),
                                NumberOfMandates = 4,
                            },
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = spWinterthurListId.ToString(),
                                NumberOfMandates = 5,
                            },
                        },
                    },
                    new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionColumnEventData()
                    {
                        UnionListId = fdpUnionListId.ToString(),
                        Cells =
                        {
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = fdpMeilenListId.ToString(),
                                NumberOfMandates = 7,
                            },
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = fdpWinterthurListId.ToString(),
                                NumberOfMandates = 6,
                            },
                        },
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        dpResult = await RunScoped<DoubleProportionalResultRepo, DoubleProportionalResult>(repo => repo.GetUnionDoubleProportionalResult(unionId)!);
        dpResult.SuperApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Completed);
        dpResult.SubApportionmentState.Should().Be(DoubleProportionalResultApportionmentState.Completed);
        dpResult.SuperApportionmentNumberOfMandates.Should().Be(60);
        dpResult.SubApportionmentNumberOfMandates.Should().Be(60);
        dpResult.NumberOfMandates.Should().Be(60);

        electionEndResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == ZhMockedData.ProportionalElectionGuidSubLotDietikon));

        simplePb = await RunOnDb(db => db.SimplePoliticalBusinesses
            .SingleAsync(x => x.Id == electionId));

        electionEndResult.ListEndResults.All(x => x.LotDecisionState is ElectionLotDecisionState.None).Should().BeTrue();
        electionEndResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();

        subApportionmentNumberOfMandatesCells = dpResult.Columns
            .SelectMany(co => co.Cells)
            .Where(ce
                => ce.SubApportionmentLotDecisionRequired
                && (ce.ListId == spMeilenListId
                || ce.ListId == spWinterthurListId
                || ce.ListId == fdpMeilenListId
                || ce.ListId == fdpWinterthurListId))
            .Select(ce => new[] { ce.SubApportionmentNumberOfMandates, ce.SubApportionmentNumberOfMandatesExclLotDecision, ce.SubApportionmentNumberOfMandatesFromLotDecision })
            .ToList();

        subApportionmentNumberOfMandatesCells.MatchSnapshot("cells-number-of-mandates-after-lot-decision");

        await AssertHasPublishedEventProcessedMessage(
            ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated.Descriptor,
            AusmittlungUuidV5.BuildPoliticalBusinessUnionEndResult(dpResult.ProportionalElectionUnionId!.Value, false));
    }

    [Fact]
    public async Task TestProcessorWithDisabledCantonSettingsEndResultFinalize()
    {
        var unionId = ZhMockedData.ProportionalElectionUnionGuidSubLot;
        var electionId = ZhMockedData.ProportionalElectionGuidSubLotDietikon;

        var spUnionListId = Guid.Parse("304d6159-4a7d-5255-bd39-4e37d7ddaf0f");
        var fdpUnionListId = Guid.Parse("c9aa5f87-de0d-5e87-8cbd-602b267e5d33");

        var spMeilenListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotMeilen][1].Item1;
        var spWinterthurListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotWinterthur][1].Item1;
        var fdpMeilenListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotMeilen][2].Item1;
        var fdpWinterthurListId = ZhMockedData.ListsByElectionIdByDpResultOwnerId[unionId][ZhMockedData.ProportionalElectionGuidSubLotWinterthur][2].Item1;

        var electionEndResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .SingleAsync(x => x.ProportionalElectionId == electionId));
        var simplePb = await RunOnDb(db => db.SimplePoliticalBusinesses
            .SingleAsync(x => x.Id == electionId));

        electionEndResult.Finalized.Should().BeFalse();
        simplePb.EndResultFinalized.Should().BeFalse();

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated
            {
                ProportionalElectionUnionId = unionId.ToString(),
                DoubleProportionalResultId = AusmittlungUuidV5.BuildDoubleProportionalResult(unionId, null, false).ToString(),
                Number = 2,
                Columns =
                {
                    new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionColumnEventData()
                    {
                        UnionListId = spUnionListId.ToString(),
                        Cells =
                        {
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = spMeilenListId.ToString(),
                                NumberOfMandates = 4,
                            },
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = spWinterthurListId.ToString(),
                                NumberOfMandates = 5,
                            },
                        },
                    },
                    new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionColumnEventData()
                    {
                        UnionListId = fdpUnionListId.ToString(),
                        Cells =
                        {
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = fdpMeilenListId.ToString(),
                                NumberOfMandates = 7,
                            },
                            new ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionCellEventData()
                            {
                                ListId = fdpWinterthurListId.ToString(),
                                NumberOfMandates = 6,
                            },
                        },
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        electionEndResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .SingleAsync(x => x.ProportionalElectionId == electionId));

        simplePb = await RunOnDb(db => db.SimplePoliticalBusinesses
            .SingleAsync(x => x.Id == electionId));

        electionEndResult.Finalized.Should().BeTrue();
        simplePb.EndResultFinalized.Should().BeTrue();
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .UpdateDoubleProportionalResultSubApportionmentLotDecisionAsync(NewValidRequest());
    }

    private UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest NewValidRequest(
        Action<UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest>? action = null)
    {
        var request = new UpdateProportionalElectionUnionDoubleProportionalResultSubApportionmentLotDecisionRequest()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdSubLot,
            Number = 2,
        };

        action?.Invoke(request);
        return request;
    }
}
