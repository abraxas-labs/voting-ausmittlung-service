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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultResetCountigCircleResultsTest : MultiResultBaseTest
{
    public ResultResetCountigCircleResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ShouldReturn()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest());

        var voteResultResettedEvents = EventPublisherMock.GetPublishedEvents<VoteResultResetted>().ToList();
        var proportionalElectionResultResettedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultResetted>().ToList();
        var majorityElectionResultResettedEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionResultResetted>().ToList();
        var ccDetailsResettedEvents = EventPublisherMock.GetPublishedEvents<ContestCountingCircleDetailsResetted>().ToList();

        voteResultResettedEvents.Should().HaveCount(1);
        proportionalElectionResultResettedEvents.Should().HaveCount(1);
        majorityElectionResultResettedEvents.Should().HaveCount(1);
        ccDetailsResettedEvents.Should().HaveCount(1);

        voteResultResettedEvents.MatchSnapshot("voteResultsResettedEvents");
        proportionalElectionResultResettedEvents.MatchSnapshot("proportionalElectionResultResettedEvents");
        majorityElectionResultResettedEvents.MatchSnapshot("majorityElectionResultResettedEvents");
        ccDetailsResettedEvents.MatchSnapshot("ccDetailsResettedEvents");
    }

    [Fact]
    public async Task ShouldResetBundleNumbers()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        await RunOnResult<MajorityElectionResultBundleNumberEntered, MajorityElectionResultAggregate>(
            MajorityElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
            aggregate =>
            {
                aggregate.DefineEntry(
                    MajorityElectionResultEntry.Detailed,
                    ContestMockedData.GuidUzwilEvoting,
                    new DomainModels.MajorityElectionResultEntryParams
                    {
                        AutomaticBallotBundleNumberGeneration = true,
                    });
                aggregate.GenerateBundleNumber(ContestMockedData.GuidUzwilEvoting);
            });

        await RunOnResult<ProportionalElectionResultBundleNumberEntered, ProportionalElectionResultAggregate>(
            ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
            aggregate =>
            {
                aggregate.DefineEntry(
                    new DomainModels.ProportionalElectionResultEntryParams
                    {
                        AutomaticBallotBundleNumberGeneration = true,
                    },
                    ContestMockedData.GuidUzwilEvoting);
                aggregate.GenerateBundleNumber(ContestMockedData.GuidUzwilEvoting);
            });

        var ballotResultId = Guid.Parse("740a9013-a26e-46d2-9efd-6a0857230bc6");

        await RunOnResult<VoteResultBundleNumberEntered, VoteResultAggregate>(
            VoteResultMockedData.GuidUzwilVoteInContestUzwilResult,
            aggregate =>
            {
                aggregate.DefineEntry(
                    VoteResultEntry.Detailed,
                    ContestMockedData.GuidUzwilEvoting,
                    new DomainModels.VoteResultEntryParams
                    {
                        AutomaticBallotBundleNumberGeneration = true,
                    });
                aggregate.GenerateBundleNumber(ballotResultId, ContestMockedData.GuidUzwilEvoting);
            });

        await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest());

        await RunOnResult<MajorityElectionResultBundleNumberEntered, MajorityElectionResultAggregate>(
            MajorityElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
            aggregate =>
            {
                var bundleNumber = aggregate.GenerateBundleNumber(ContestMockedData.GuidUzwilEvoting);
                bundleNumber.Should().Be(1);
            });

        await RunOnResult<ProportionalElectionResultBundleNumberEntered, ProportionalElectionResultAggregate>(
            ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil,
            aggregate =>
            {
                var bundleNumber = aggregate.GenerateBundleNumber(ContestMockedData.GuidUzwilEvoting);
                bundleNumber.Should().Be(1);
            });

        await RunOnResult<VoteResultBundleNumberEntered, VoteResultAggregate>(
            VoteResultMockedData.GuidUzwilVoteInContestUzwilResult,
            aggregate =>
            {
                var bundleNumber = aggregate.GenerateBundleNumber(ballotResultId, ContestMockedData.GuidUzwilEvoting);
                bundleNumber.Should().Be(1);
            });
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionOngoing)]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    public async Task ShouldReturnWithCorrectState(CountingCircleResultState state)
    {
        await SetResultState(state);

        await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest());

        var voteResultResettedEvents = EventPublisherMock.GetPublishedEvents<VoteResultResetted>().ToList();
        var proportionalElectionResultResettedEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultResetted>().ToList();
        var majorityElectionResultResettedEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionResultResetted>().ToList();
        var ccDetailsResettedEvents = EventPublisherMock.GetPublishedEvents<ContestCountingCircleDetailsResetted>().ToList();

        voteResultResettedEvents.Should().HaveCount(1);
        proportionalElectionResultResettedEvents.Should().HaveCount(1);
        majorityElectionResultResettedEvents.Should().HaveCount(1);
        ccDetailsResettedEvents.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task ShouldThrowWithMonitoringState(CountingCircleResultState state)
    {
        await SetResultState(state);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot reset results when there are any audited or plausibilised results");
    }

    [Fact]
    public async Task ShouldThrowWithInitialState()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot reset results when there are any initial results");
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestId.ToString(), async () =>
        {
            await SetResultState(CountingCircleResultState.SubmissionOngoing);

            await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest());

            var voteResultResettedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<VoteResultResetted>().ToList();
            var proportionalElectionResultResettedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<ProportionalElectionResultResetted>().ToList();
            var majorityElectionResultResettedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<MajorityElectionResultResetted>().ToList();
            var ccDetailsResettedEvents = EventPublisherMock.GetPublishedEventsWithMetadata<ContestCountingCircleDetailsResetted>().ToList();

            voteResultResettedEvents.Should().HaveCount(1);
            proportionalElectionResultResettedEvents.Should().HaveCount(1);
            majorityElectionResultResettedEvents.Should().HaveCount(1);
            ccDetailsResettedEvents.Should().HaveCount(1);

            return voteResultResettedEvents
                .Concat(proportionalElectionResultResettedEvents)
                .Concat(majorityElectionResultResettedEvents)
                .Concat(ccDetailsResettedEvents)
                .ToArray();
        });
    }

    [Fact]
    public async Task ShouldThrowIfNotCountingCircleManager()
    {
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
    }

    [Fact]
    public async Task ShoudThrowAfterTestingPhaseEnded()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        await ModifyDbEntities<Contest>(
            c => c.Id == ContestId,
            c => c.State = ContestState.Active);

        await RunOnDb(async db =>
        {
            db.ContestCountingCircleDetails.Add(new()
            {
                Id = AusmittlungUuidV5.BuildContestCountingCircleDetails(ContestId, CountingCircleId, true),
                ContestId = ContestId,
                CountingCircleId = CountingCircleId,
            });

            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest testing phase has ended");
    }

    [Fact]
    public async Task ShouldThrowWithMissingCountingCircleDetails()
    {
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.ResetCountingCircleResultsAsync(NewValidRequest(x =>
            {
                x.ContestId = ContestMockedData.IdStGallenEvoting;
                x.CountingCircleId = CountingCircleMockedData.IdStGallen;
            })),
            StatusCode.InvalidArgument,
            "Counting circle details aggregate is not initialized yet");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);
        await new ResultService.ResultServiceClient(channel)
            .ResetCountingCircleResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ResetCountingCircleResultsRequest NewValidRequest(Action<ResetCountingCircleResultsRequest>? action = null)
    {
        var request = new ResetCountingCircleResultsRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        };

        action?.Invoke(request);
        return request;
    }
}
