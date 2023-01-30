// (c) Copyright 2022 by Abraxas Informatik AG
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
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultEnterManualListEndResultTest : ProportionalElectionEndResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";

    public ProportionalElectionEndResultEnterManualListEndResultTest(TestApplicationFactory factory)
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
        var endResultId = "e51853c0-e16c-4143-b629-5ab58ec14637";

        await ModifyDbEntities<ProportionalElectionEndResult>(
            r => r.ProportionalElectionId == ProportionalElectionEndResultMockedData.ElectionGuid,
            r => r.Finalized = true);

        await ModifyDbEntities<ProportionalElectionListEndResult>(
            r => r.ListId == Guid.Parse(ProportionalElectionEndResultMockedData.ListId4),
            r => r.NumberOfMandates = 0);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionManualListEndResultEntered
            {
                ProportionalElectionEndResultId = endResultId,
                ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId4,
                CandidateEndResults =
                {
                    new ProportionalElectionManualCandidateEndResultEventData
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId1,
                        State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                    },
                    new ProportionalElectionManualCandidateEndResultEventData
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                        State = SharedProto.ProportionalElectionCandidateEndResultState.NotElected,
                    },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        });
        endResult.Finalized.Should().BeFalse();

        var listEndResult = endResult.ListEndResults.Single(x => x.List.Id == ProportionalElectionEndResultMockedData.ListId4);
        listEndResult.NumberOfMandates.Should().Be(1);
        listEndResult.MatchSnapshot("response");
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await SetAllAuditedTentatively();
        await SetManualRequired();

        await MonitoringElectionAdminClient.EnterManualListEndResultAsync(NewValidRequest());
        var eventData = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionManualListEndResultEntered>();
        eventData.MatchSnapshot("event");
    }

    [Fact]
    public async Task TestShouldReturnAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = ProportionalElectionEndResultMockedData.ElectionGuid;
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        // testing phase
        await SetAllAuditedTentatively();
        await SetManualRequired();

        await MonitoringElectionAdminClient.EnterManualListEndResultAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionManualListEndResultEntered>();
        await RunEvents<ProportionalElectionManualListEndResultEntered>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.ProportionalElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);

        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == electionId,
            e =>
            {
                e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles;
                e.ManualEndResultRequired = true;
            });

        await MonitoringElectionAdminClient.EnterManualListEndResultAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionManualListEndResultEntered>();
        await RunEvents<ProportionalElectionManualListEndResultEntered>();

        evTestingPhaseEnded.ProportionalElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await SetAllAuditedTentatively();
            await SetManualRequired();
            await MonitoringElectionAdminClient.EnterManualListEndResultAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionManualListEndResultEntered>();
        });
    }

    [Fact]
    public async Task TestShouldThrowIfCandidateIsMissing()
    {
        await SetAllAuditedTentatively();
        await SetManualRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.EnterManualListEndResultAsync(NewValidRequest(x =>
            {
                x.CandidateEndResults.Clear();
                x.CandidateEndResults.Add(new EnterProportionalElectionManualCandidateEndResultRequest
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                    State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                });
            })),
            StatusCode.InvalidArgument,
            "All candidate end results of a list must be provided");
    }

    [Fact]
    public async Task TestShouldThrowIfCandidateDuplicate()
    {
        await SetAllAuditedTentatively();
        await SetManualRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.EnterManualListEndResultAsync(NewValidRequest(x =>
            {
                x.CandidateEndResults.Clear();
                x.CandidateEndResults.Add(new EnterProportionalElectionManualCandidateEndResultRequest
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                    State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                });
                x.CandidateEndResults.Add(new EnterProportionalElectionManualCandidateEndResultRequest
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                    State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                });
            })),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Fact]
    public async Task TestShouldThrowIfInvalidCandidateState()
    {
        await SetAllAuditedTentatively();
        await SetManualRequired();
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.EnterManualListEndResultAsync(new EnterProportionalElectionManualListEndResultRequest
            {
                ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId4,
                CandidateEndResults =
                {
                    new EnterProportionalElectionManualCandidateEndResultRequest
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId1,
                        State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                    },
                    new EnterProportionalElectionManualCandidateEndResultRequest
                    {
                        CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                        State = SharedProto.ProportionalElectionCandidateEndResultState.Pending,
                    },
                },
            }),
            StatusCode.InvalidArgument,
            "Invalid candidate end result state");
    }

    [Fact]
    public async Task TestShouldThrowIfNoManualEndResultRequired()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.EnterManualListEndResultAsync(NewValidRequest(x =>
            {
                x.CandidateEndResults.Clear();
                x.CandidateEndResults.Add(new EnterProportionalElectionManualCandidateEndResultRequest
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                    State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                });
            })),
            StatusCode.PermissionDenied,
            "Cannot enter a manual end result for election");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetAllAuditedTentatively();
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.EnterManualListEndResultAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.EnterManualListEndResultAsync(
                NewValidRequest(x => x.ProportionalElectionListId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await CreateService("unknown-tenant", roles: RolesMockedData.MonitoringElectionAdmin).EnterManualListEndResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .EnterManualListEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private EnterProportionalElectionManualListEndResultRequest NewValidRequest(
        Action<EnterProportionalElectionManualListEndResultRequest>? customizer = null)
    {
        var r = new EnterProportionalElectionManualListEndResultRequest
        {
            ProportionalElectionListId = ProportionalElectionEndResultMockedData.ListId4,
            CandidateEndResults =
            {
                new EnterProportionalElectionManualCandidateEndResultRequest()
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId1,
                    State = SharedProto.ProportionalElectionCandidateEndResultState.Elected,
                },
                new EnterProportionalElectionManualCandidateEndResultRequest()
                {
                    CandidateId = ProportionalElectionEndResultMockedData.List4CandidateId2,
                    State = SharedProto.ProportionalElectionCandidateEndResultState.NotElected,
                },
            },
        };
        customizer?.Invoke(r);
        return r;
    }

    private async Task SetManualRequired()
    {
        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.ManualEndResultRequired = true);
    }
}
