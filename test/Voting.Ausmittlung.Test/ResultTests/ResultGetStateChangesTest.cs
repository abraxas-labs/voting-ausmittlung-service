// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultGetStateChangesTest : BaseTest<ResultService.ResultServiceClient>
{
    public ResultGetStateChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = MonitoringElectionAdminClient.GetStateChanges(
            new GetResultStateChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        var uzwilId = Guid.Parse(VoteResultMockedData.IdUzwilVoteInContestStGallenResult);
        var uzwilCcId = CountingCircleMockedData.GuidUzwil;
        var uwzilPbId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen);

        // this should be processed
        await PublishMessage(
            new ResultStateChanged(
                uzwilId,
                uzwilCcId,
                uwzilPbId,
                CountingCircleResultState.AuditedTentatively));

        // this should be ignored (no access)
        await PublishMessage(
            new ResultStateChanged(
                Guid.Parse(VoteResultMockedData.IdGossauVoteInContestGossauResult),
                CountingCircleMockedData.GuidGossau,
                Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau),
                CountingCircleResultState.AuditedTentatively));

        // this should be processed
        await PublishMessage(
            new ResultStateChanged(
                uzwilId,
                uzwilCcId,
                uwzilPbId,
                CountingCircleResultState.Plausibilised));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(2);
        responses.All(x => x.Id == VoteResultMockedData.IdUzwilVoteInContestStGallenResult).Should().BeTrue();

        var expectedCountingCircleResultStates = new[]
        {
                CountingCircleResultState.AuditedTentatively,
                CountingCircleResultState.Plausibilised,
        };

        // sort since order is not guaranteed.
        responses
            .Select(x => x.NewState)
            .OrderBy(x => x)
            .Should()
            .BeEquivalentTo(
                expectedCountingCircleResultStates,
                opt => opt.ComparingEnumsByValue());

        callCts.Cancel();
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var responseStream = MonitoringElectionAdminClient.GetStateChanges(
                    new GetResultStateChangesRequest
                    {
                        ContestId = ContestMockedData.IdGossau,
                    },
                    new CallOptions(cancellationToken: cts.Token));

                await responseStream.ResponseStream.MoveNext();
            },
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ResultService.ResultServiceClient(channel).GetStateChanges(
            new GetResultStateChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.MoveNext();
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
