// (c) Copyright 2022 by Abraxas Informatik AG
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
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultResetCountigCircleResultsTest : BaseTest<ResultService.ResultServiceClient>
{
    private static readonly Guid ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting);
    private static readonly Guid CountingCircleId = CountingCircleMockedData.GuidUzwil;

    private static readonly Guid VoteId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestUzwilWithoutChilds);
    private static readonly Guid ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds);
    private static readonly Guid MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds);

    private static readonly Guid VoteResultId = VoteResultMockedData.GuidUzwilVoteInContestUzwilResult;
    private static readonly Guid ProportionalElectionResultId = ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil;
    private static readonly Guid MajorityElectionResultId = MajorityElectionResultMockedData.GuidUzwilElectionResultInContestUzwil;

    public ResultResetCountigCircleResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
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
        await new ResultService.ResultServiceClient(channel)
            .ResetCountingCircleResultsAsync(NewValidRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
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

    private async Task SetResultState(CountingCircleResultState state)
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var db = sp.GetRequiredService<DataContext>();
            var aggregateFactory = sp.GetRequiredService<IAggregateFactory>();
            var aggregateRepo = sp.GetRequiredService<AggregateRepositoryMock>();

            await SetResultState(aggregateRepo, aggregateFactory, state);

            var ccResults = await db.SimpleCountingCircleResults
                .AsTracking()
                .Where(cc => cc.CountingCircle!.SnapshotContestId == ContestId)
                .ToListAsync();

            foreach (var ccResult in ccResults)
            {
                ccResult.State = state;
            }

            await db.SaveChangesAsync();
        });
    }

    private async Task SetResultState(IAggregateRepository aggregateRepo, IAggregateFactory aggregateFactory, CountingCircleResultState state)
    {
        switch (state)
        {
            case CountingCircleResultState.SubmissionOngoing:
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.StartSubmission(CountingCircleId, VoteId, ContestId, false));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.StartSubmission(CountingCircleId, ProportionalElectionId, ContestId, false));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.StartSubmission(CountingCircleId, MajorityElectionId, ContestId, false));
                break;
            case CountingCircleResultState.SubmissionDone:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.SubmissionOngoing);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.SubmissionFinished(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.SubmissionFinished(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.SubmissionFinished(ContestId));
                break;
            case CountingCircleResultState.ReadyForCorrection:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.SubmissionDone);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.FlagForCorrection(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.FlagForCorrection(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.FlagForCorrection(ContestId));
                break;
            case CountingCircleResultState.CorrectionDone:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.ReadyForCorrection);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.CorrectionFinished(string.Empty, ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.CorrectionFinished(string.Empty, ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.CorrectionFinished(string.Empty, ContestId));
                break;
            case CountingCircleResultState.AuditedTentatively:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.SubmissionDone);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.AuditedTentatively(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.AuditedTentatively(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.AuditedTentatively(ContestId));
                break;
            case CountingCircleResultState.Plausibilised:
                await SetResultState(aggregateRepo, aggregateFactory, CountingCircleResultState.AuditedTentatively);
                await ExecuteOnAggregate<VoteResultAggregate>(aggregateRepo, aggregateFactory, VoteResultId, x => x.Plausibilise(ContestId));
                await ExecuteOnAggregate<ProportionalElectionResultAggregate>(aggregateRepo, aggregateFactory, ProportionalElectionResultId, x => x.Plausibilise(ContestId));
                await ExecuteOnAggregate<MajorityElectionResultAggregate>(aggregateRepo, aggregateFactory, MajorityElectionResultId, x => x.Plausibilise(ContestId));
                break;
        }
    }

    private async Task ExecuteOnAggregate<TAggregate>(
        IAggregateRepository aggregateRepo,
        IAggregateFactory aggregateFactory,
        Guid id,
        Action<TAggregate> action)
        where TAggregate : BaseEventSourcingAggregate
    {
        var aggregate = await aggregateRepo.TryGetById<TAggregate>(id)
            ?? aggregateFactory.New<TAggregate>();
        action.Invoke(aggregate);
        await aggregateRepo.Save(aggregate);
    }
}
