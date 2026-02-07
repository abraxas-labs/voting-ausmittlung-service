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
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using ElectionLotDecisionState = Abraxas.Voting.Ausmittlung.Services.V1.Models.ElectionLotDecisionState;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultAuditedTentativelyTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultAuditedTentatively>().MatchSnapshot();
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultPublished>().ElectionResultId.Should().Be(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen);
    }

    [Fact]
    public async Task TestShouldReturnWithoutPublish()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.Gossau.Id && x.SnapshotContestId == ContestMockedData.StGallenEvotingUrnengang.Id,
            x => x.Type = DomainOfInfluenceType.Bz);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetPublishedEvents<VoteResultPublished>().Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultAuditedTentatively>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await RunToState(CountingCircleResultState.SubmissionDone);
            await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultAuditedTentatively>();
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
    public async Task TestShouldThrowContestLocked()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
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
    public async Task TestShouldThrowDuplicate()
    {
        await RunToState(CountingCircleResultState.AuditedTentatively);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.AuditedTentativelyAsync(
                NewValidRequest(x => x.ElectionResultIds.Add(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen))),
            StatusCode.InvalidArgument,
            "duplicate");
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
    public async Task TestProcessorWithEnabledImplcitMandateDistribution()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 500;

            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultAuditedTentatively
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            EventInfo = GetMockedEventInfo(),
        });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        var ccResult = await RunOnDb(db => db.ProportionalElectionResults.SingleAsync(x => x.Id == Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen)));
        ccResult.State.Should().Be(CountingCircleResultState.AuditedTentatively);

        endResult.MatchSnapshot();
        endResult.MandateDistributionTriggered.Should().BeTrue();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.LotDecisionState is ElectionLotDecisionState.OpenAndRequired).Should().BeTrue();

        var id = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        await AssertHasPublishedEventProcessedMessage(ProportionalElectionResultAuditedTentatively.Descriptor, id);
    }

    [Fact]
    public async Task TestProcessorWithEnabledImplcitMandateDistributionAndMultipleAuditedTentatively()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 500;

            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultAuditedTentatively
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            EventInfo = GetMockedEventInfo(),
        });

        // A end result can have multiple counting circles. It counts as a "DoneCountingCircle" after the submission is finished.
        // In previous event versions we have the behavior of "implicite mandate distribution" which gets
        // triggered on "AuditedTentatively after "AllCountingCirclesDone".
        // An end result should only trigger the mandate distribution on the 1st "AuditedTentatively"
        // (and the 2nd one should not throw an exception).
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultAuditedTentatively
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            EventInfo = GetMockedEventInfo(),
        });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.MandateDistributionTriggered.Should().BeTrue();
    }

    [Fact]
    public async Task TestProcessorWithUnionDpAlgorithmAndEnabledImplicitMandateDistribution()
    {
        ResetDb();
        await ZhMockedData.Seed(RunScoped);

        var electionGuid = ZhMockedData.ProportionalElectionGuidKtratWinterthur;
        var ccResultGuid = AusmittlungUuidV5.BuildPoliticalBusinessResult(electionGuid, ZhMockedData.CountingCircleGuidWinterthur, false);

        await TestEventPublisher.Publish(
            new ProportionalElectionResultAuditedTentatively
            {
                ElectionResultId = ccResultGuid.ToString(),
                EventInfo = GetMockedEventInfo(),
            });

        var endResult = await RunOnDb(db => db.ProportionalElectionEndResult
            .AsSplitQuery()
            .Include(x => x.ListEndResults)
            .ThenInclude(x => x.CandidateEndResults)
            .SingleAsync(x => x.ProportionalElectionId == electionGuid));

        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.AllCountingCirclesDone.Should().BeTrue();

        // Number of mandates should never be distributed by a election event with the union dp algorithm.
        endResult.MandateDistributionTriggered.Should().BeFalse();
        endResult.ListEndResults.Should().NotBeEmpty();
        endResult.ListEndResults.All(l => l.NumberOfMandates == 0).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.Any()).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => x.State == ProportionalElectionCandidateEndResultState.Pending)).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => x.Rank == 1)).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => !x.LotDecisionEnabled)).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => !x.LotDecisionRequired)).Should().BeTrue();
        endResult.ListEndResults.All(l => l.LotDecisionState != Data.Models.ElectionLotDecisionState.OpenAndRequired).Should().BeTrue();

        var unionEndResult = await RunOnDb(db => db.ProportionalElectionUnionEndResults
            .Include(x => x.ProportionalElectionUnion.DoubleProportionalResult)
            .SingleAsync(x => x.ProportionalElectionUnionId == ZhMockedData.ProportionalElectionUnionGuidKtrat));
        unionEndResult.CountOfDoneElections.Should().Be(3);
        unionEndResult.ProportionalElectionUnion.DoubleProportionalResult.Should().BeNull();
    }

    [Fact]
    public async Task TestProcessorWithNonUnionDpAlgorithmAndEnabledImplcitMandateDistribution()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ProportionalElection)
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            endResult.ProportionalElection.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum;

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 500;

            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultAuditedTentatively
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            EventInfo = GetMockedEventInfo(),
        });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.MatchSnapshot("endResult");
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.MandateDistributionTriggered.Should().BeTrue();
        endResult.ListEndResults.Any(l => l.NumberOfMandates != 0).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.CandidateEndResults.Any(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Elected)).Should().BeTrue();
        endResult.ListEndResults.All(l => l.LotDecisionState is ElectionLotDecisionState.None).Should().BeTrue();

        var dpResult = await MonitoringElectionAdminClient.GetDoubleProportionalResultAsync(new GetProportionalElectionDoubleProportionalResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });
        dpResult.MatchSnapshot("dpResult");
    }

    [Fact]
    public async Task TestProcessorWithManualEndResultAndEnabledImplcitMandateDistribution()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 1000;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 0;

            await db.SaveChangesAsync();
        });

        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultAuditedTentatively
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            EventInfo = GetMockedEventInfo(),
        });

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.MatchSnapshot();
        endResult.ManualEndResultRequired.Should().BeTrue();
        endResult.MandateDistributionTriggered.Should().BeTrue();

        endResult.ListEndResults.Any().Should().BeTrue();
        endResult.ListEndResults.All(l => l.NumberOfMandates == 0).Should().BeTrue();
        endResult.ListEndResults.All(l => l.CandidateEndResults.All(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.NotElected)).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.LotDecisionState is ElectionLotDecisionState.OpenAndRequired).Should().BeTrue();
    }

    [Fact]
    public async Task TestProcessor()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);

        await RunOnDb(async db =>
        {
            var endResult = await db.ProportionalElectionEndResult
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.ListEndResults.OrderBy(y => y.List.OrderNumber))
                .ThenInclude(x => x.CandidateEndResults)
                .SingleAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen));

            var listEndResults = endResult.ListEndResults.ToList();
            listEndResults[0].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[1].ConventionalSubTotal.UnmodifiedListVotesCount = 500;
            listEndResults[2].ConventionalSubTotal.UnmodifiedListVotesCount = 500;

            await db.SaveChangesAsync();
        });

        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultAuditedTentatively>();

        await AssertCurrentState(CountingCircleResultState.AuditedTentatively);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
        });

        endResult.MatchSnapshot();
        endResult.ManualEndResultRequired.Should().BeFalse();
        endResult.Finalized.Should().BeFalse();
        endResult.MandateDistributionTriggered.Should().BeFalse();
        endResult.ListEndResults.Any().Should().BeTrue();
        endResult.ListEndResults.All(l => l.NumberOfMandates == 0).Should().BeTrue();

        var candidateEndResults = endResult.ListEndResults.SelectMany(l => l.CandidateEndResults).ToList();
        candidateEndResults.Any().Should().BeTrue();
        candidateEndResults.All(x => x.State == SharedProto.ProportionalElectionCandidateEndResultState.Pending).Should().BeTrue();
        endResult.ListEndResults.Any(l => l.LotDecisionState is ElectionLotDecisionState.OpenAndRequired).Should().BeFalse();

        var id = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        await AssertHasPublishedEventProcessedMessage(ProportionalElectionResultAuditedTentatively.Descriptor, id);
    }

    [Fact]
    public async Task TestProcessorWithDisabledCantonSettingsEndResultFinalize()
    {
        var electionGuid = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen);
        await RunToState(CountingCircleResultState.SubmissionDone);

        await ModifyDbEntities<ContestCantonDefaults>(
            _ => true,
            x => x.EndResultFinalizeDisabled = true,
            splitQuery: true);

        await ModifyDbEntities<ProportionalElectionEndResult>(
            x => x.ProportionalElectionId == electionGuid,
            x => x.CountOfDoneCountingCircles = x.TotalCountOfCountingCircles - 1);

        await MonitoringElectionAdminClient.AuditedTentativelyAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultAuditedTentatively>();

        await AssertCurrentState(CountingCircleResultState.AuditedTentatively);

        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(new GetProportionalElectionEndResultRequest
        {
            ProportionalElectionId = electionGuid.ToString(),
        });

        // Proportional elections only implicitly finalize after mandate distribution is done.
        endResult.Finalized.Should().BeFalse();
        endResult.MandateDistributionTriggered.Should().BeFalse();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .AuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    private ProportionalElectionResultAuditedTentativelyRequest NewValidRequest(Action<ProportionalElectionResultAuditedTentativelyRequest>? customizer = null)
    {
        var r = new ProportionalElectionResultAuditedTentativelyRequest
        {
            ElectionResultIds =
            {
                ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            },
        };
        customizer?.Invoke(r);
        return r;
    }
}
