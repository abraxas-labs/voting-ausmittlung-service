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

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultResetToSubmissionFinishedTest : VoteResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public VoteResultResetToSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultResettedToSubmissionFinished>().MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<VoteResultUnpublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnWithUnpublishWhenNoBeforeAuditedPublish()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultUnpublished>().VoteResultId.Should().Be(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
    }

    [Fact]
    public async Task TestShouldReturnWithNoUnpublishWhenBeforeAuditedPublish()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();

        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x =>
            {
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);

        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<VoteResultUnpublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultResettedToSubmissionFinished>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(
                new VoteResultResetToSubmissionFinishedRequest
                {
                    VoteResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(
                new VoteResultResetToSubmissionFinishedRequest
                {
                    VoteResultId = IdBadFormat,
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
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        // mock some eVoting data to test it is attached correctly to the end result
        await ModifyDbEntities(
            (BallotQuestionResult r) => r.QuestionId == Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
            r =>
            {
                r.EVotingSubTotal.TotalCountOfAnswerYes = 10;
                r.EVotingSubTotal.TotalCountOfAnswerNo = 20;
                r.EVotingSubTotal.TotalCountOfAnswerUnspecified = 5;
            });
        await ModifyDbEntities(
            (TieBreakQuestionResult r) => r.QuestionId == Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
            r =>
            {
                r.EVotingSubTotal.TotalCountOfAnswerQ1 = 11;
                r.EVotingSubTotal.TotalCountOfAnswerQ2 = 21;
                r.EVotingSubTotal.TotalCountOfAnswerUnspecified = 6;
            });
        await ModifyDbEntities(
            (BallotQuestionEndResult r) => r.QuestionId == Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
            r =>
            {
                r.EVotingSubTotal.TotalCountOfAnswerYes = 10;
                r.EVotingSubTotal.TotalCountOfAnswerNo = 20;
                r.EVotingSubTotal.TotalCountOfAnswerUnspecified = 5;
            });
        await ModifyDbEntities(
            (TieBreakQuestionEndResult r) => r.QuestionId == Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
            r =>
            {
                r.EVotingSubTotal.TotalCountOfAnswerQ1 = 11;
                r.EVotingSubTotal.TotalCountOfAnswerQ2 = 21;
                r.EVotingSubTotal.TotalCountOfAnswerUnspecified = 6;
            });

        await RunOnDb(async db =>
        {
            var vote = await db.VoteEndResults.AsTracking().FirstAsync(x => x.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen));
            vote.Finalized = true;
            await db.SaveChangesAsync();
        });

        // mock some cc detail data and set cc result states to ongoing excet the current one, to test whether aggregated details get updated.
        await RunOnDb(async db =>
        {
            var vcs = await db.VotingCardResultDetails.AsTracking()
                .Where(vc => vc.ContestCountingCircleDetails.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidGossau)
                .ToListAsync();

            foreach (var vc in vcs)
            {
                vc.CountOfReceivedVotingCards = 100000;
            }

            var simpleResults = await db.SimpleCountingCircleResults.AsTracking()
                .Where(ccr => ccr.CountingCircle!.BasisCountingCircleId == CountingCircleMockedData.GuidGossau
                    && ccr.Id != VoteResultMockedData.GuidGossauVoteInContestStGallenResult)
                .ToListAsync();

            foreach (var simpleResult in simpleResults)
            {
                simpleResult.State = CountingCircleResultState.SubmissionOngoing;
            }

            await db.SaveChangesAsync();
        });

        var doiDetailsBefore = await RunOnDb(
            db => db.DomainOfInfluences
                .AsSplitQuery()
                .Include(x => x.Details)
                .ThenInclude(x => x!.VotingCards)
                .Include(x => x.Details)
                .ThenInclude(x => x!.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.SnapshotContestId == ContestMockedData.GuidStGallenEvoting && x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id));

        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        await RunEvents<VoteResultResettedToSubmissionFinished>();

        await AssertCurrentState(CountingCircleResultState.SubmissionDone);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetVoteEndResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
        });

        endResult.Finalized.Should().BeTrue();
        endResult.MatchSnapshot();

        var doiDetailsAfter = await RunOnDb(
            db => db.DomainOfInfluences
                .AsSplitQuery()
                .Include(x => x.Details)
                .ThenInclude(x => x!.VotingCards)
                .Include(x => x.Details)
                .ThenInclude(x => x!.CountOfVotersInformationSubTotals)
                .SingleAsync(x => x.SnapshotContestId == ContestMockedData.GuidStGallenEvoting && x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallen.Id));

        // the count should remain unchanged on state change audited -> submisson done.
        EnsureValidAggregatedVotingCards(
            doiDetailsBefore.Details!.VotingCards,
            doiDetailsAfter.Details!.VotingCards,
            x => x.DomainOfInfluenceType == DomainOfInfluenceType.Ct && x.Valid && x.Channel == VotingChannel.BallotBox,
            0);

        var id = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult);
        await AssertHasPublishedEventProcessedMessage(VoteResultResettedToSubmissionFinished.Descriptor, id);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new VoteResultService.VoteResultServiceClient(channel)
            .ResetToSubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private VoteResultResetToSubmissionFinishedRequest NewValidRequest()
    {
        return new VoteResultResetToSubmissionFinishedRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        };
    }
}
