// (c) Copyright by Abraxas Informatik AG
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
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultUpdateListLotDecisionsTest : ProportionalElectionEndResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionEndResultUpdateListLotDecisionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();
    }

    [Fact]
    public async Task TestProcessor()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionListEndResultListLotDecisionsUpdated
            {
                ProportionalElectionEndResultId = endResultId,
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ListLotDecisions =
                {
                    new ProportionalElectionEndResultListLotDecisionEventData
                    {
                        Entries =
                        {
                            new ProportionalElectionEndResultListLotDecisionEntryEventData
                            {
                                ListId = ProportionalElectionEndResultMockedData.ListId1,
                                Winning = true,
                            },
                            new ProportionalElectionEndResultListLotDecisionEntryEventData
                            {
                                ListId = ProportionalElectionEndResultMockedData.ListId2,
                                Winning = false,
                            },
                        },
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(
            new GetProportionalElectionEndResultRequest
            {
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
            });
        var listLotDecisions = endResult.ListLotDecisions;
        listLotDecisions.MatchSnapshot("response");
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListEndResultListLotDecisionsUpdated>();
        eventData.MatchSnapshot("event", x => x.ProportionalElectionEndResultId);
    }

    [Fact]
    public async Task TestShouldReturnAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        // testing phase
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListEndResultListLotDecisionsUpdated>();
        await RunEvents<ProportionalElectionListEndResultListLotDecisionsUpdated>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.ProportionalElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);

        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == electionId,
            e =>
            {
                e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles;
                e.MandateDistributionTriggered = true;
                e.ManualEndResultRequired = true;
            });

        await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionListEndResultListLotDecisionsUpdated>();
        await RunEvents<ProportionalElectionListEndResultListLotDecisionsUpdated>();

        evTestingPhaseEnded.ProportionalElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await SetAllAuditedTentatively();
            await TriggerMandateDistribution();
            await SetManualEndResultRequired();
            await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionListEndResultListLotDecisionsUpdated>();
        });
    }

    [Fact]
    public async Task TestShouldThrowIfNoLotDecisionEntries()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries.Clear())),
            StatusCode.InvalidArgument,
            "a list lot decision must have at least 2 entries");
    }

    [Fact]
    public async Task TestShouldThrowIfNoWinningLotDecisionEntries()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries[0].Winning = false)),
            StatusCode.InvalidArgument,
            "a list lot decision must have at least 1 winning entry");
    }

    [Fact]
    public async Task TestShouldThrowIfNoListIdOrListUnionId()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries[0].ListId = string.Empty)),
            StatusCode.InvalidArgument,
            "a list lot decision entry must have a list id or list union id");
    }

    [Fact]
    public async Task TestShouldThrowIfListIdAndListUnionId()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries[0].ListUnionId = "57c90dea-bcf2-4f23-9124-e999812e5bdc")),
            StatusCode.InvalidArgument,
            "a list lot decision entry can only have a list id or list union id");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicatedListId()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x =>
                {
                    x.ListLotDecisions[0].Entries
                        .Add(new UpdateProportionalElectionEndResultListLotDecisionEntryRequest
                        {
                            ListId = ProportionalElectionEndResultMockedData.ListId1,
                            Winning = false,
                        });
                })),
            StatusCode.InvalidArgument,
            "a list id or list union id may only appear once in the lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfDuplicatedListUnionId()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries[2].ListUnionId = ProportionalElectionEndResultMockedData.ListUnionId1)),
            StatusCode.InvalidArgument,
            "a list id or list union id may only appear once in the lot decisions");
    }

    [Fact]
    public async Task TestShouldThrowIfListIdNotExists()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries[0].ListId = "655941a8-751f-4e81-adcc-80314b703f5b")),
            StatusCode.InvalidArgument,
            "a list id or list union id found which not exists in available lists");
    }

    [Fact]
    public async Task TestShouldThrowIfListUnionIdNotExists()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ListLotDecisions[0].Entries[1].ListUnionId = "655941a8-751f-4e81-adcc-80314b703f5b")),
            StatusCode.InvalidArgument,
            "a list id or list union id found which not exists in available lists");
    }

    [Fact]
    public async Task TestShouldThrowIfElectionMandateDistributionNotStarted()
    {
        await SetAllAuditedTentatively();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "lot decisions are not allowed on this end result");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await SetAllAuditedTentatively();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ProportionalElectionId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(
                NewValidRequest(x => x.ProportionalElectionId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowIfManualEndResultNotRequired()
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateEndResultListLotDecisionsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "list lot decisions not allowed for non manual end result required");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await SetAllAuditedTentatively();
        await TriggerMandateDistribution();
        await SetManualEndResultRequired();
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .UpdateEndResultListLotDecisionsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private async Task SetManualEndResultRequired()
    {
        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == ProportionalElectionEndResultMockedData.ElectionGuid,
            e => e.ManualEndResultRequired = true);
    }

    private UpdateProportionalElectionEndResultListLotDecisionsRequest NewValidRequest(
        Action<UpdateProportionalElectionEndResultListLotDecisionsRequest>? customizer = null)
    {
        var r = new UpdateProportionalElectionEndResultListLotDecisionsRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
            ListLotDecisions =
            {
                new UpdateProportionalElectionEndResultListLotDecisionRequest
                {
                    Entries =
                    {
                        new UpdateProportionalElectionEndResultListLotDecisionEntryRequest
                        {
                            ListId = ProportionalElectionEndResultMockedData.ListId1,
                            Winning = true,
                        },
                        new UpdateProportionalElectionEndResultListLotDecisionEntryRequest
                        {
                            ListUnionId = ProportionalElectionEndResultMockedData.ListUnionId1,
                            Winning = false,
                        },
                        new UpdateProportionalElectionEndResultListLotDecisionEntryRequest
                        {
                            ListUnionId = ProportionalElectionEndResultMockedData.SubListUnionId1,
                            Winning = false,
                        },
                    },
                },
            },
        };
        customizer?.Invoke(r);
        return r;
    }
}
