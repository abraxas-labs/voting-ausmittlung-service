// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultDefineEntryTest : ProportionalElectionResultBaseTest
{
    public ProportionalElectionResultDefineEntryTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunOnDb(async db =>
        {
            db.ProportionalElectionBundles.Add(new ProportionalElectionResultBundle
            {
                ListId = Guid.Parse(ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen),
                Number = 1,
                ElectionResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen,
            });
            db.ProportionalElectionBundles.Add(new ProportionalElectionResultBundle
            {
                Number = 2,
                ElectionResultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen,
            });
            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultEntryDefined
            {
                ResultEntryParams = new ProportionalElectionResultEntryParamsEventData
                {
                    BallotBundleSize = 10,
                    BallotBundleSampleSize = 5,
                    BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                    AutomaticEmptyVoteCounting = true,
                    AutomaticBallotBundleNumberGeneration = true,
                },
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                EventInfo = GetMockedEventInfo(),
            });

        var entry = await RunOnDb(db => db.ProportionalElectionResults
                .FirstAsync(x => x.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen));
        entry.MatchSnapshot(x => x.CountingCircleId);

        var hasResults = await RunOnDb(db => db.ProportionalElectionUnmodifiedListResults
            .AnyAsync(x =>
                x.Result.ProportionalElectionId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen
                && x.VoteCount != 0));
        hasResults.Should().BeFalse();

        var hasBundles = await RunOnDb(db => db.ProportionalElectionBundles
            .AnyAsync(x => x.ElectionResult.ProportionalElectionId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen));
        hasBundles.Should().BeFalse();
    }

    [Fact]
    public async Task TestShouldBeOk()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultEntryDefined>();
        });
    }

    [Fact]
    public async Task TestShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await StGallenErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunOnDb(async db =>
        {
            db.ProportionalElectionBundles.RemoveRange(db.ProportionalElectionBundles);
            await db.SaveChangesAsync();
        });
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestGreaterBallotBundleSampleSizeShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x =>
                x.ResultEntryParams.BallotBundleSampleSize = x.ResultEntryParams.BallotBundleSize + 1)),
            StatusCode.InvalidArgument,
            "'Ballot Bundle Sample Size' must be less than or equal to '10'");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.ProportionalElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));
            election.EnforceEmptyVoteCountingForCountingCircles = true;
            election.AutomaticEmptyVoteCounting = false;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced AutomaticEmptyVoteCounting setting not respected");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedReviewProcedureSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.ProportionalElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));
            election.EnforceReviewProcedureForCountingCircles = true;
            election.ReviewProcedure = ProportionalElectionReviewProcedure.Physically;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced ReviewProcedure setting not respected");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedCandidateCheckDigitSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var election = await db.ProportionalElections
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));
            election.EnforceCandidateCheckDigitForCountingCircles = true;
            election.CandidateCheckDigit = false;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "enforced CandidateCheckDigit setting not respected");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .DefineEntryAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
    }

    private DefineProportionalElectionResultEntryRequest NewValidRequest(Action<DefineProportionalElectionResultEntryRequest>? customizer = null)
    {
        var r = new DefineProportionalElectionResultEntryRequest
        {
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotBundleSampleSize = 5,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        };
        customizer?.Invoke(r);
        return r;
    }
}
