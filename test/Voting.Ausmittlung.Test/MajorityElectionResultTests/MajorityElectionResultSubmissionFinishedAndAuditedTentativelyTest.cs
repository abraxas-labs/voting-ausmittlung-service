// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultSubmissionFinishedAndAuditedTentativelyTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultSubmissionFinishedAndAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
        var events = new List<IMessage>
        {
            EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionFinished>(),
            EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultAuditedTentatively>(),
        };
        events.MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<MajorityElectionResultPublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnWithPublish()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id && x.SnapshotContestId == ContestMockedData.GuidBundesurnengang,
            x => x.Type = DomainOfInfluenceType.Mu);
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultPublished>().ElectionResultId.Should().Be(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithTooManyBallotsInDeletedBundle()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SeedBallots(BallotBundleState.Deleted);
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionOngoing);
            await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
            return new[]
            {
                EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultSubmissionFinished>(),
                EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultAuditedTentatively>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldThrowForNonSelfOwnedPoliticalBusiness()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
                }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithBundlesAllDoneOrDeleted()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var bundles = await db.MajorityElectionResultBundles.AsTracking().ToListAsync();
            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            bundles[0].State = BallotBundleState.Deleted;
            await db.SaveChangesAsync();
        });
        await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .SubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest(Action<MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest>? customizer = null)
    {
        var req = new MajorityElectionResultSubmissionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        };
        customizer?.Invoke(req);
        return req;
    }
}
