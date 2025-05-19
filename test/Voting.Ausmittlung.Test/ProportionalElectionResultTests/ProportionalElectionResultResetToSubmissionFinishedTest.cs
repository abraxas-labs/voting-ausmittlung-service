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

public class ProportionalElectionResultResetToSubmissionFinishedTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultResetToSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultResettedToSubmissionFinished>().MatchSnapshot();
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultUnpublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnWithUnpublishWhenNoBeforeAuditedPublish()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await RunToPublished();
        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultUnpublished>().ElectionResultId.Should().Be(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen);
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
                x.ManualPublishResultsEnabled = false;
                x.PublishResultsBeforeAuditedTentatively = true;
            },
            splitQuery: true);

        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<ProportionalElectionResultUnpublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.AuditedTentatively);
            await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultResettedToSubmissionFinished>();
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
                new ProportionalElectionResultResetToSubmissionFinishedRequest
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
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(
                new ProportionalElectionResultResetToSubmissionFinishedRequest
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
            async () => await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);

        await RunOnDb(async db =>
        {
            var proportionalElectionEndResult = await db.ProportionalElectionEndResult.AsTracking().FirstAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund));
            proportionalElectionEndResult.Finalized = true;
            proportionalElectionEndResult.ManualEndResultRequired = true;
            await db.SaveChangesAsync();
        });

        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultResettedToSubmissionFinished>();

        await AssertCurrentState(CountingCircleResultState.SubmissionDone);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.Finalized.Should().BeFalse();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.MatchSnapshot();

        var id = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        await AssertHasPublishedEventProcessedMessage(ProportionalElectionResultResettedToSubmissionFinished.Descriptor, id);
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
            new ProportionalElectionResultResettedToSubmissionFinished
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

        await MonitoringElectionAdminClient.ResetToSubmissionFinishedAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultResettedToSubmissionFinished>();

        await AssertCurrentState(CountingCircleResultState.SubmissionDone);

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
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .ResetToSubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private ProportionalElectionResultResetToSubmissionFinishedRequest NewValidRequest()
    {
        return new ProportionalElectionResultResetToSubmissionFinishedRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        };
    }
}
