// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultGetListTest : BaseTest<ResultService.ResultServiceClient>
{
    public ResultGetListTest(TestApplicationFactory factory)
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
        await PermissionMockedData.Seed(RunScoped);
        EventPublisherMock.Clear();
    }

    [Fact]
    public async Task TestShouldReturn()
    {
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.ContestId == Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang),
            x => x.CountingMachine = CountingMachine.None);

        var response = await ErfassungCreatorClient.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestMockedData.IdVergangenerBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });
        response.MatchSnapshot();
        EventPublisherMock.AllPublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManager()
    {
        var contestId = ContestMockedData.IdStGallenEvoting;
        var countingCircleId = CountingCircleMockedData.IdGossau;
        var request = new GetResultListRequest
        {
            ContestId = contestId,
            CountingCircleId = countingCircleId,
        };

        var response = await StGallenErfassungElectionAdminClient.GetListAsync(request);
        response.CurrentTenantIsResponsible.Should().BeTrue();
        response.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response.Results.Any().Should().BeTrue();
        response.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = contestId });

        var response2 = await StGallenErfassungElectionAdminClient.GetListAsync(request);
        response2.CurrentTenantIsResponsible.Should().BeFalse();

        response.Results.Count.Should().Be(response2.Results.Count);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestMockedData.IdVergangenerBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestMockedData.IdVergangenerBundesurnengang,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });
        response.MatchSnapshot();
        EventPublisherMock.AllPublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldThrowNoAccess()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdVergangenerBundesurnengang,
                CountingCircleId = CountingCircleMockedData.IdBund,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldReturnAndUpdateStateAsErfassungElectionAdmin()
    {
        var client = CreateService(
            SecureConnectTestDefaults.MockedTenantUzwil.Id,
            SecureConnectTestDefaults.MockedUserDefault.Loginid,
            RolesMockedData.ErfassungElectionAdmin);
        var response = await client.GetListAsync(new GetResultListRequest
        {
            ContestId = ContestMockedData.IdUzwilEVoting,
            CountingCircleId = CountingCircleMockedData.IdUzwil,
        });
        response.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAndUpdateStateWithStatisticDataAsErfassungElectionAdmin()
    {
        var contestId = ContestMockedData.IdGossau;
        var countingCircleId = CountingCircleMockedData.IdGossau;

        var response = await ErfassungElectionAdminClient.GetListAsync(new GetResultListRequest
        {
            ContestId = contestId,
            CountingCircleId = countingCircleId,
        });
        response.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();
        response.MatchSnapshot();

        EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionStarted>();
        await RunEvents<VoteResultSubmissionStarted>(false);

        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultSubmissionStarted>();
        await RunEvents<ProportionalElectionResultSubmissionStarted>(false);

        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionStarted>();
        await RunEvents<MajorityElectionResultSubmissionStarted>();

        // event should published only for the first request
        var response2 = await ErfassungElectionAdminClient.GetListAsync(new GetResultListRequest
        {
            ContestId = contestId,
            CountingCircleId = countingCircleId,
        });
        response2.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response2.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();
        EventPublisherMock.AllPublishedEvents.Count().Should().Be(0);
    }

    [Fact]
    public async Task TestShouldResetResultsWhenStateDoesNotMatch()
    {
        var contestId = ContestMockedData.IdGossau;
        var countingCircleId = CountingCircleMockedData.IdGossau;

        var response = await ErfassungElectionAdminClient.GetListAsync(new GetResultListRequest
        {
            ContestId = contestId,
            CountingCircleId = countingCircleId,
        });
        response.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();

        EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionStarted>();
        await RunEvents<VoteResultSubmissionStarted>(false);

        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultSubmissionStarted>();
        await RunEvents<ProportionalElectionResultSubmissionStarted>(false);

        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionStarted>();
        await RunEvents<MajorityElectionResultSubmissionStarted>(false);

        // Simulate results being deleted and re-created
        var affectedRows = await RunOnDb(db =>
            db.SimpleCountingCircleResults
                .Where(x => x.PoliticalBusiness!.ContestId == ContestMockedData.GuidGossau)
                .ExecuteUpdateAsync(setter => setter.SetProperty(x => x.State, CountingCircleResultState.Initial)));
        affectedRows.Should().Be(3);

        // event should reset the results
        var response2 = await ErfassungElectionAdminClient.GetListAsync(new GetResultListRequest
        {
            ContestId = contestId,
            CountingCircleId = countingCircleId,
        });
        response2.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response2.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();
        EventPublisherMock.AllPublishedEvents.Count().Should().Be(6);

        EventPublisherMock.GetSinglePublishedEvent<VoteResultResetted>();
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultResetted>();
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultResetted>();
    }

    [Fact]
    public async Task TestShouldWorkAfterTestingPhaseEnded()
    {
        var contestId = Guid.Parse(ContestMockedData.IdGossau);
        var countingCircleId = Guid.Parse(CountingCircleMockedData.IdGossau);
        var request = new GetResultListRequest
        {
            ContestId = contestId.ToString(),
            CountingCircleId = countingCircleId.ToString(),
        };

        // testing phase
        var response = await ErfassungElectionAdminClient.GetListAsync(request);
        response.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();

        var voteStartedInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionStarted>();
        var voteId = Guid.Parse(voteStartedInTestingPhase.VoteId);
        var voteResultInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessResult(voteId, countingCircleId, false);
        await RunEvents<VoteResultSubmissionStarted>(false);

        var proportionalElectionStartedInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultSubmissionStarted>();
        var proportionalElectionId = Guid.Parse(proportionalElectionStartedInTestingPhase.ElectionId);
        var proportionalElectionInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessResult(proportionalElectionId, countingCircleId, false);
        await RunEvents<ProportionalElectionResultSubmissionStarted>(false);

        var majorityElectionStartedInTestingPhase = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionStarted>();
        var majorityElectionId = Guid.Parse(majorityElectionStartedInTestingPhase.ElectionId);
        var majorityElectionInTestingPhaseId = AusmittlungUuidV5.BuildPoliticalBusinessResult(majorityElectionId, countingCircleId, false);
        await RunEvents<MajorityElectionResultSubmissionStarted>();

        voteStartedInTestingPhase.VoteResultId.Should().Be(
            voteResultInTestingPhaseId.ToString());
        proportionalElectionStartedInTestingPhase.ElectionResultId.Should().Be(
            proportionalElectionInTestingPhaseId.ToString());
        majorityElectionStartedInTestingPhase.ElectionResultId.Should().Be(
            majorityElectionInTestingPhaseId.ToString());

        // testing phase ended
        await TestEventPublisher.Publish(GetNextEventNumber(), new ContestTestingPhaseEnded { ContestId = contestId.ToString() });

        var response2 = await ErfassungElectionAdminClient.GetListAsync(request);
        response2.State.Should().HaveSameValueAs(CountingCircleResultState.SubmissionOngoing);
        response2.Results.All(x => x.State == ProtoModels.CountingCircleResultState.SubmissionOngoing).Should().BeTrue();

        var voteStartedTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<VoteResultSubmissionStarted>();
        var voteResultTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessResult(voteId, countingCircleId, true);
        await RunEvents<VoteResultSubmissionStarted>(false);

        var proportionalElectionStartedTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultSubmissionStarted>();
        var proportionalElectionTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessResult(proportionalElectionId, countingCircleId, true);
        await RunEvents<ProportionalElectionResultSubmissionStarted>(false);

        var majorityElectionStartedTestingPhaseEnded = EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultSubmissionStarted>();
        var majorityElectionTestingPhaseEndedId = AusmittlungUuidV5.BuildPoliticalBusinessResult(majorityElectionId, countingCircleId, true);
        await RunEvents<MajorityElectionResultSubmissionStarted>();

        voteStartedTestingPhaseEnded.VoteResultId.Should().Be(
            voteResultTestingPhaseEndedId.ToString());
        proportionalElectionStartedTestingPhaseEnded.ElectionResultId.Should().Be(
            proportionalElectionTestingPhaseEndedId.ToString());
        majorityElectionStartedTestingPhaseEnded.ElectionResultId.Should().Be(
            majorityElectionTestingPhaseEndedId.ToString());
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultService.ResultServiceClient(channel)
            .GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdGossau,
                CountingCircleId = CountingCircleMockedData.IdGossau,
            });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);
}
