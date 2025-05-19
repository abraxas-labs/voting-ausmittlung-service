// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultAuditedTentativelyTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultAuditedTentatively>().MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultPublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnWithPublish()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id && x.SnapshotContestId == ContestMockedData.GuidBundesurnengang,
            x => x.Type = DomainOfInfluenceType.Mu);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultPublished>().ElectionResultId.Should().Be(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund);
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultAuditedTentatively>();
        });
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdNotFound))),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(IdBadFormat))),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                new MajorityElectionResultAuditedTentativelyRequest
                {
                    ElectionResultIds =
                    {
                        MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
                    },
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund))),
            StatusCode.InvalidArgument,
            "duplicate");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        await RunEvents<MajorityElectionResultAuditedTentatively>();

        await AssertCurrentState(CountingCircleResultState.AuditedTentatively);

        var id = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultAuditedTentatively.Descriptor, id);

        var endResult = await RunOnDb(db => db.MajorityElectionEndResults
            .SingleAsync(r => r.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund)));
        endResult.Finalized.Should().BeFalse();
    }

    [Fact]
    public async Task TestProcessorWithDisabledCantonSettingsEndResultFinalize()
    {
        var electionGuid = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund);
        await RunToState(CountingCircleResultState.SubmissionDone);

        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = true,
            splitQuery: true);

        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElectionId == electionGuid,
            x => x.CountOfDoneCountingCircles = x.TotalCountOfCountingCircles - 1);

        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        await RunEvents<MajorityElectionResultAuditedTentatively>();

        var endResult = await RunOnDb(db => db.MajorityElectionEndResults
            .SingleAsync(r => r.MajorityElectionId == electionGuid));
        endResult.Finalized.Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .AuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private MajorityElectionResultAuditedTentativelyRequest NewValidRequest(Action<MajorityElectionResultAuditedTentativelyRequest>? customizer = null)
    {
        var r = new MajorityElectionResultAuditedTentativelyRequest
        {
            ElectionResultIds =
            {
                MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            },
        };
        customizer?.Invoke(r);
        return r;
    }
}
