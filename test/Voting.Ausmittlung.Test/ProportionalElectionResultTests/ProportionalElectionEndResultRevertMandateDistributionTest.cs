// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultRevertMandateDistributionTest : ProportionalElectionEndResultBaseTest
{
    private bool _initialAuthCall = true;

    public ProportionalElectionEndResultRevertMandateDistributionTest(TestApplicationFactory factory)
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
        await TriggerMandateDistributionPerRequest();
        await MonitoringElectionAdminClient.RevertEndResultMandateDistributionAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionReverted>();
        ev.ProportionalElectionId.Should().Be(ProportionalElectionEndResultMockedData.ElectionId);
        ev.ProportionalElectionEndResultId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await TriggerMandateDistributionPerRequest();
            await MonitoringElectionAdminClient.RevertEndResultMandateDistributionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionEndResultMandateDistributionReverted>();
        });
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var request = NewValidRequest();
        var electionId = Guid.Parse(request.ProportionalElectionId);
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);

        await TriggerMandateDistributionPerRequest();
        await MonitoringElectionAdminClient.RevertEndResultMandateDistributionAsync(request);
        var evInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionReverted>();
        await RunEvents<ProportionalElectionEndResultMandateDistributionReverted>();

        var endResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, false);
        evInTestingPhase.ProportionalElectionEndResultId.Should().Be(endResultInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        await ModifyDbEntities<ProportionalElectionEndResult>(
            e => e.ProportionalElectionId == electionId,
            e => e.CountOfDoneCountingCircles = e.TotalCountOfCountingCircles);

        await TriggerMandateDistributionPerRequest();
        await MonitoringElectionAdminClient.RevertEndResultMandateDistributionAsync(request);
        var evTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionReverted>();
        await RunEvents<ProportionalElectionEndResultMandateDistributionReverted>();

        var endResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionId, true);
        evTestingPhaseEnded.ProportionalElectionEndResultId.Should().Be(endResultTestingPhaseEndedId.ToString());
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await TriggerMandateDistributionPerRequest();
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.RevertEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task ShouldThrowIfNotCalculated()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.RevertEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot revert mandate distribution, if it is not triggered yet");
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await CreateService("unknown-tenant", roles: RolesMockedData.MonitoringElectionAdmin).RevertEndResultMandateDistributionAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestProcessor()
    {
        var electionGuid = Guid.Parse(ProportionalElectionEndResultMockedData.ElectionId);
        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == electionGuid,
            x =>
            {
                x.Finalized = true;
                x.MandateDistributionTriggered = true;
            });

        var result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(r => r.ProportionalElectionId == electionGuid));

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionEndResultMandateDistributionReverted
            {
                ProportionalElectionId = electionGuid.ToString(),
                ProportionalElectionEndResultId = result.Id.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        result = await RunOnDb(x => x.ProportionalElectionEndResult.FirstAsync(vr => vr.ProportionalElectionId == electionGuid));
        result.MandateDistributionTriggered.Should().BeFalse();
        result.Finalized.Should().BeFalse();

        await AssertHasPublishedEventProcessedMessage(
            ProportionalElectionEndResultMandateDistributionReverted.Descriptor,
            result.Id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        if (_initialAuthCall)
        {
            await TriggerMandateDistributionPerRequest();
            _initialAuthCall = false;
        }

        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .RevertEndResultMandateDistributionAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private RevertProportionalElectionEndResultMandateDistributionRequest NewValidRequest()
    {
        return new()
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        };
    }

    private async Task TriggerMandateDistributionPerRequest()
    {
        await MonitoringElectionAdminClient.StartEndResultMandateDistributionAsync(new StartProportionalElectionEndResultMandateDistributionRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionEndResultMandateDistributionStarted>());
    }
}
