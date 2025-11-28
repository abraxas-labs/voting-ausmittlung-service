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
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultFlagForCorrectionTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultFlagForCorrectionTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminWithoutComment()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest(x => x.Comment = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithNoUnpublishWhenBeforeAuditedPublish()
    {
        await ModifyDbEntities<ContestCantonDefaults>(
            x => x.ContestId == ContestMockedData.GuidStGallenEvoting,
            x =>
            {
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);

        await RunToState(CountingCircleResultState.SubmissionDone);
        await RunToPublished();

        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultUnpublished>().Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultFlaggedForCorrection>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultFlaggedForCorrection>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(
                new ProportionalElectionResultFlagForCorrectionRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(
                new ProportionalElectionResultFlagForCorrectionRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
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
            async () => await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertSubmissionDoneTimestamp(true);
        await AssertReadyForCorrectionTimestamp(false);

        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultFlaggedForCorrection>();

        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);
        await AssertSubmissionDoneTimestamp(false);
        await AssertReadyForCorrectionTimestamp(true);
        var comments = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen)
            .ToListAsync());
        comments.MatchSnapshot(x => x.Id);

        await AssertHasPublishedEventProcessedMessage(ProportionalElectionResultFlaggedForCorrection.Descriptor, ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen);
    }

    [Fact]
    public async Task TestProcessorWithoutComment()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest(req => req.Comment = string.Empty));
        await RunEvents<ProportionalElectionResultFlaggedForCorrection>();

        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);
        var comment = await RunOnDb(db => db.CountingCircleResultComments
            .Where(x => x.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen)
            .FirstOrDefaultAsync());
        comment.Should().BeNull();
    }

    [Fact]
    public async Task TestProcessorWithUnionDpResult()
    {
        ResetDb();
        await ZhMockedData.Seed(RunScoped, true);

        // seed some random end result data to test whether they get resetted on all elections
        // (and not only the election where we reset one counting circle result).
        await ModifyDbEntities<ProportionalElectionCandidateEndResult>(
            _ => true,
            x =>
            {
                x.Rank = 1;
                x.LotDecisionEnabled = true;
                x.LotDecisionRequired = true;
                x.State = ProportionalElectionCandidateEndResultState.Elected;
            });

        await ModifyDbEntities<ProportionalElectionEndResult>(
            _ => true,
            x => x.Finalized = true);

        var endResultInSameUnion = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == ZhMockedData.ProportionalElectionGuidKtratDietikon));
        endResultInSameUnion.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeTrue();

        var unionEndResult = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .Include(x => x.ProportionalElectionUnion.DoubleProportionalResult)
            .SingleAsync(x => x.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));
        unionEndResult.CountOfDoneElections.Should().Be(3);
        unionEndResult.ProportionalElectionUnion.DoubleProportionalResult.Should().NotBeNull();

        var ccResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(ZhMockedData.ProportionalElectionGuidKtratWinterthur, ZhMockedData.CountingCircleGuidWinterthur, false);

        await TestEventPublisher.Publish(
            new ProportionalElectionResultFlaggedForCorrection
            {
                ElectionResultId = ccResultGuid.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        endResultInSameUnion = await RunOnDb(db => db.ProportionalElectionEndResult
                    .AsSplitQuery()
                    .Include(x => x.ListEndResults)
                    .ThenInclude(x => x.CandidateEndResults)
                    .SingleAsync(x => x.ProportionalElectionId == ZhMockedData.ProportionalElectionGuidKtratDietikon));

        endResultInSameUnion.AllCountingCirclesDone.Should().BeTrue();
        endResultInSameUnion.Finalized.Should().BeFalse();

        // A reset should always influence the whole union. If one result resets from audited to submission finished
        // it will delete the double proportional result and reset all end results in the union.
        endResultInSameUnion.ListEndResults.Any().Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => l.NumberOfMandates == 0).Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => l.CandidateEndResults.Any()).Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => l.CandidateEndResults.All(x => x.State == ProportionalElectionCandidateEndResultState.Pending)).Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => l.CandidateEndResults.All(x => x.Rank == 0)).Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => l.CandidateEndResults.All(x => !x.LotDecisionEnabled)).Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => l.CandidateEndResults.All(x => !x.LotDecisionRequired)).Should().BeTrue();
        endResultInSameUnion.ListEndResults.All(l => !l.HasOpenRequiredLotDecisions).Should().BeTrue();

        unionEndResult = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .Include(x => x.ProportionalElectionUnion.DoubleProportionalResult)
            .SingleAsync(x => x.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));
        unionEndResult.CountOfDoneElections.Should().Be(2);
        unionEndResult.ProportionalElectionUnion.DoubleProportionalResult.Should().BeNull();
    }

    [Fact]
    public async Task TestProcessorWithNonUnionDpResult()
    {
        var electionGuid = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen);

        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == electionGuid,
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        await RunToState(CountingCircleResultState.AuditedTentatively);
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionEndResultMandateDistributionStarted
        {
            EventInfo = GetMockedEventInfo(),
            ProportionalElectionId = electionGuid.ToString(),
            ProportionalElectionEndResultId = AusmittlungUuidV5.BuildPoliticalBusinessEndResult(electionGuid, false).ToString(),
        });

        await RunOnDb(async db =>
        {
            var proportionalElectionEndResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .FirstAsync(x => x.ProportionalElectionId == electionGuid);
            proportionalElectionEndResult.Finalized = true;
            proportionalElectionEndResult.ManualEndResultRequired = true;
            await db.SaveChangesAsync();
        });

        (await RunOnDb(db => db.DoubleProportionalResults.AnyAsync(x => x.ProportionalElectionId == electionGuid)))
            .Should().BeTrue();

        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(new ProportionalElectionResultResetToSubmissionFinishedRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        await RunEvents<ProportionalElectionResultResettedToSubmissionFinished>();
        await MonitoringElectionAdminClient.FlagForCorrectionAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultFlaggedForCorrection>();

        await AssertCurrentState(CountingCircleResultState.ReadyForCorrection);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        endResult.Finalized.Should().BeFalse();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.MatchSnapshot();

        (await RunOnDb(db => db.DoubleProportionalResults.AnyAsync(x => x.ProportionalElectionId == electionGuid)))
            .Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .FlagForCorrectionAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private ProportionalElectionResultFlagForCorrectionRequest NewValidRequest(
        Action<ProportionalElectionResultFlagForCorrectionRequest>? customizer = null)
    {
        var req = new ProportionalElectionResultFlagForCorrectionRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            Comment = "my-comment",
        };
        customizer?.Invoke(req);
        return req;
    }

    private async Task AssertSubmissionDoneTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.ProportionalElectionResults.SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen)));
        (result.SubmissionDoneTimestamp != null).Should().Be(hasTimestamp);
    }

    private async Task AssertReadyForCorrectionTimestamp(bool hasTimestamp)
    {
        var result = await RunOnDb(db => db.ProportionalElectionResults.SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen)));
        (result.ReadyForCorrectionTimestamp != null).Should().Be(hasTimestamp);
    }
}
