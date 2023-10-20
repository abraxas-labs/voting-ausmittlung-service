// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultPrepareSubmissionFinishedTest : MultiResultBaseTest
{
    public ResultPrepareSubmissionFinishedTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        // ensure that the tenant is only the cc manager and not the contest manager
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            x => x.SecureConnectId = "random-id");

        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        var response = await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(NewValidRequest());
        response.Id.Should().NotBeEmpty();
        response.Code.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        // ensure that the tenant is only the contest manager
        await ModifyDbEntities<Authority>(
            x => x.CountingCircle!.BasisCountingCircleId == CountingCircleId,
            x => x.SecureConnectId = "random-id");

        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        var response = await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(NewValidRequest());
        response.Id.Should().BeEmpty();
        response.Code.Should().BeEmpty();
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
            async () => await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowResultNotFound()
    {
        await SetResultState(CountingCircleResultState.SubmissionOngoing);

        var response = await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(NewValidRequest());
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(
                NewValidRequest(x => x.CountingCircleResultIds.Add(VoteResultMockedData.IdUzwilVoteInContestStGallenResult))),
            StatusCode.InvalidArgument,
            "Non existing result id provided");
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
            async () => await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(NewValidRequest()),
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
            async () => await ErfassungElectionAdminClient.PrepareSubmissionFinishedAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultService.ResultServiceClient(channel)
            .PrepareSubmissionFinishedAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return NoRole;
    }

    private CountingCircleResultsPrepareSubmissionFinishedRequest NewValidRequest(Action<CountingCircleResultsPrepareSubmissionFinishedRequest>? action = null)
    {
        var request = new CountingCircleResultsPrepareSubmissionFinishedRequest()
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
            CountingCircleResultIds =
            {
                VoteResultId.ToString(),
                ProportionalElectionResultId.ToString(),
                MajorityElectionResultId.ToString(),
            },
        };

        action?.Invoke(request);
        return request;
    }
}
