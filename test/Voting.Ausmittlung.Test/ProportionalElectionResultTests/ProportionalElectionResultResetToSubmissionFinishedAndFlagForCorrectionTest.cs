// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest());
        var events = new List<IMessage>
        {
            EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultResettedToSubmissionFinished>(),
            EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultFlaggedForCorrection>(),
        };
        events.MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultUnpublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnWithUnpublishWhenNoBeforeAuditedPublish()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultUnpublished>().ElectionResultId.Should().Be(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest());
            return
            [
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultResettedToSubmissionFinished>(),
                EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultFlaggedForCorrection>()
            ];
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(
                new ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(
                new ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowForNonCommunalPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.Gossau.Id && x.SnapshotContestId == ContestMockedData.GuidStGallenEvoting,
            x => x.Type = DomainOfInfluenceType.Ct);

        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "reset to submission finished and flag for correction is not allowed for non communal political business");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .ResetToSubmissionFinishedAndFlagForCorrectionAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionRequest NewValidRequest()
    {
        return new ProportionalElectionResultResetToSubmissionFinishedAndFlagForCorrectionRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        };
    }
}