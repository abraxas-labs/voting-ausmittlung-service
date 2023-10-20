// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultSubmissionFinishedTest : MultiResultBaseTest
{
    public ResultSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SecondFactorTransactionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        // ensure that the tenant is only the cc manager and not the contest manager
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            x => x.SecureConnectId = "random-id");

        await SetResultState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());

        var voteResultEvents = EventPublisherMock.GetPublishedEvents<VoteResultSubmissionFinished>().ToList();
        var proportionalElectionResultEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultSubmissionFinished>().ToList();
        var majorityElectionResultEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionResultSubmissionFinished>().ToList();

        voteResultEvents.Should().HaveCount(1);
        proportionalElectionResultEvents.Should().HaveCount(1);
        majorityElectionResultEvents.Should().HaveCount(1);

        voteResultEvents.MatchSnapshot(nameof(voteResultEvents));
        proportionalElectionResultEvents.MatchSnapshot(nameof(proportionalElectionResultEvents));
        majorityElectionResultEvents.MatchSnapshot(nameof(majorityElectionResultEvents));
    }

    [Fact]
    public async Task TestShouldReturnWithEmptySecondFactorId()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest(x => x.SecondFactorTransactionId = string.Empty));

        var voteResultEvents = EventPublisherMock.GetPublishedEvents<VoteResultSubmissionFinished>().ToList();
        var proportionalElectionResultEvents = EventPublisherMock.GetPublishedEvents<ProportionalElectionResultSubmissionFinished>().ToList();
        var majorityElectionResultEvents = EventPublisherMock.GetPublishedEvents<MajorityElectionResultSubmissionFinished>().ToList();

        voteResultEvents.Should().HaveCount(1);
        proportionalElectionResultEvents.Should().HaveCount(1);
        majorityElectionResultEvents.Should().HaveCount(1);

        voteResultEvents.MatchSnapshot(nameof(voteResultEvents));
        proportionalElectionResultEvents.MatchSnapshot(nameof(proportionalElectionResultEvents));
        majorityElectionResultEvents.MatchSnapshot(nameof(majorityElectionResultEvents));
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        // ensure that the tenant is only the contest manager
        await ModifyDbEntities<Authority>(
            x => x.CountingCircle!.BasisCountingCircleId == CountingCircleId,
            x => x.SecureConnectId = "random-id");

        await SetResultState(CountingCircleResultState.SubmissionOngoing);
        await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        // ensure that the tenant is only the contest manager
        await ModifyDbEntities<Authority>(
            x => x.CountingCircle!.BasisCountingCircleId == CountingCircleId,
            x => x.SecureConnectId = "random-id");

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = ContestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        await SetResultState(CountingCircleResultState.SubmissionOngoing);
        await SetContestState(ContestId.ToString(), ContestState.Active);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowResultNotFound()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        var response = await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest());
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(
                NewValidRequest(x => x.CountingCircleResultIds.Add(VoteResultMockedData.IdUzwilVoteInContestStGallenResult))),
            StatusCode.InvalidArgument,
            "Non existing counting circle result id provided");
    }

    [Theory]
    [InlineData(CountingCircleResultState.Initial)]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.ReadyForCorrection)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task ShouldThrowWithWrongResultState(CountingCircleResultState state)
    {
        await SetResultState(state);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "Cannot finish submission when there are results which are not");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        // testing phase ended
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = ContestId.ToString() });
        await RunEvents<ContestTestingPhaseEnded>();

        await SetContestState(ContestId.ToString(), ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.SubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultService.ResultServiceClient(channel)
            .SubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return NoRole;
    }

    private CountingCircleResultsSubmissionFinishedRequest NewValidRequest(Action<CountingCircleResultsSubmissionFinishedRequest>? action = null)
    {
        var request = new CountingCircleResultsSubmissionFinishedRequest()
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
            CountingCircleResultIds =
            {
                VoteResultId.ToString(),
                ProportionalElectionResultId.ToString(),
                MajorityElectionResultId.ToString(),
            },
            SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
        };

        action?.Invoke(request);
        return request;
    }
}
