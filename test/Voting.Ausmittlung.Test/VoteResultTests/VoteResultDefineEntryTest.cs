// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultDefineEntryTest : VoteResultBaseTest
{
    public VoteResultDefineEntryTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    [Fact]
    public async Task TestProcessorDetailEntry()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultEntryDefined
            {
                ResultEntry = SharedProto.VoteResultEntry.Detailed,
                ResultEntryParams = new VoteResultEntryParamsEventData
                {
                    AutomaticBallotBundleNumberGeneration = false,
                    BallotBundleSampleSizePercent = 50,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                },
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestGossauResult,
                EventInfo = GetMockedEventInfo(),
            },
            new VoteResultEntryDefined
            {
                ResultEntry = SharedProto.VoteResultEntry.Detailed,
                ResultEntryParams = new VoteResultEntryParamsEventData
                {
                    AutomaticBallotBundleNumberGeneration = true,
                    BallotBundleSampleSizePercent = 20,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                },
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                EventInfo = GetMockedEventInfo(),
            });

        var entries = await RunOnDb(db =>
            db.VoteResults
                .AsSplitQuery()
                .Include(x => x.Results).ThenInclude(x => x.QuestionResults)
                .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults)
                .Where(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestGossauResult)
                            || x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
                .ToListAsync());

        OrderVoteResults(entries);
        entries.MatchSnapshot(x => x.CountingCircleId);

        ShouldHaveResettedConventionalResults(entries);
    }

    [Fact]
    public async Task TestProcessorFinalResults()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultEntryDefined
            {
                ResultEntry = SharedProto.VoteResultEntry.FinalResults,
                ResultEntryParams = new VoteResultEntryParamsEventData
                {
                    AutomaticBallotBundleNumberGeneration = false,
                    BallotBundleSampleSizePercent = 50,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                },
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestGossauResult,
                EventInfo = GetMockedEventInfo(),
            },
            new VoteResultEntryDefined
            {
                ResultEntry = SharedProto.VoteResultEntry.FinalResults,
                ResultEntryParams = new VoteResultEntryParamsEventData
                {
                    AutomaticBallotBundleNumberGeneration = true,
                    BallotBundleSampleSizePercent = 20,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                },
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                EventInfo = GetMockedEventInfo(),
            });

        var entries = await RunOnDb(db =>
            db.VoteResults
                .AsSplitQuery()
                .Include(x => x.Results).ThenInclude(x => x.QuestionResults)
                .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults)
                .Where(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestGossauResult)
                            || x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult))
                .ToListAsync());

        OrderVoteResults(entries);
        entries.MatchSnapshot(x => x.CountingCircleId);

        ShouldHaveResettedConventionalResults(entries);
    }

    [Fact]
    public async Task TestShouldBeOk()
    {
        await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await StGallenErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultEntryDefined>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultEntryDefined>();
        });
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

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowInvalidResultEntry()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x =>
                x.ResultEntry = (SharedProto.VoteResultEntry)100)),
            StatusCode.InvalidArgument,
            "ResultEntry");
    }

    [Fact]
    public async Task TestNoRespectForEnforcedReviewProcedureSettingShouldThrow()
    {
        await RunOnDb(async db =>
        {
            var vote = await db.Votes
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
            vote.EnforceReviewProcedureForCountingCircles = true;
            vote.ReviewProcedure = VoteReviewProcedure.Physically;
            await db.SaveChangesAsync();
        });
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.DefineEntryAsync(NewValidRequest(x =>
            {
                x.ResultEntry = SharedProto.VoteResultEntry.Detailed;
                x.ResultEntryParams = new DefineVoteResultEntryParamsRequest
                {
                    AutomaticBallotBundleNumberGeneration = true,
                    BallotBundleSampleSizePercent = 10,
                    ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
                };
            })),
            StatusCode.InvalidArgument,
            "enforced ReviewProcedure setting not respected");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .DefineEntryAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private DefineVoteResultEntryRequest NewValidRequest(Action<DefineVoteResultEntryRequest>? customizer = null)
    {
        var r = new DefineVoteResultEntryRequest
        {
            ResultEntry = SharedProto.VoteResultEntry.FinalResults,
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        };
        customizer?.Invoke(r);
        return r;
    }

    private void ShouldHaveResettedConventionalResults(IEnumerable<VoteResult> voteResults)
    {
        // all results except e voting should be reset
        foreach (var result in voteResults)
        {
            var defaultValue = result.Entry == VoteResultEntry.Detailed ? 0 : (int?)null;

            result.Results.Any(r =>
                    r.QuestionResults.Any(qr =>
                        qr.ConventionalSubTotal.TotalCountOfAnswerNo != defaultValue ||
                        qr.ConventionalSubTotal.TotalCountOfAnswerYes != defaultValue ||
                        qr.ConventionalSubTotal.TotalCountOfAnswerUnspecified != defaultValue) ||
                    r.TieBreakQuestionResults.Any(qr =>
                        qr.ConventionalSubTotal.TotalCountOfAnswerQ1 != defaultValue ||
                        qr.ConventionalSubTotal.TotalCountOfAnswerQ2 != defaultValue ||
                        qr.ConventionalSubTotal.TotalCountOfAnswerUnspecified != defaultValue))
                .Should().BeFalse();
        }
    }

    private void OrderVoteResults(IEnumerable<VoteResult> voteResults)
    {
        foreach (var ballotResult in voteResults.SelectMany(result => result.Results))
        {
            ballotResult.QuestionResults = ballotResult.QuestionResults.OrderBy(x => x.Id).ToList();
        }
    }
}
