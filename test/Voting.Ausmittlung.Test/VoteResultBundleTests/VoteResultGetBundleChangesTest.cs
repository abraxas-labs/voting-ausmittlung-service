// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultGetBundleChangesTest : BaseTest<VoteResultBundleService.VoteResultBundleServiceClient>
{
    public VoteResultGetBundleChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await VoteResultBundleMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = MonitoringElectionAdminClient.GetBundleChanges(
            new GetVoteResultBundleChangesRequest
            {
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        var gossauId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult);

        // this should be processed
        await PublishMessage(
            new VoteBundleChanged(
                Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
                gossauId));

        // this should be ignored (no access)
        await PublishMessage(
            new VoteBundleChanged(
                Guid.Parse(VoteResultBundleMockedData.IdUzwilBundle1),
                Guid.Parse(VoteResultMockedData.IdUzwilVoteInContestStGallenBallotResult)));

        // this should be processed
        await PublishMessage(
            new VoteBundleChanged(
                Guid.Parse(VoteResultBundleMockedData.IdGossauBundle2),
                gossauId));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(2);
        responses.Count(x => x.Id == VoteResultBundleMockedData.IdGossauBundle1).Should().Be(1);
        responses.Count(x => x.Id == VoteResultBundleMockedData.IdGossauBundle2).Should().Be(1);

        callCts.Cancel();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));
        var responseStream = new VoteResultBundleService.VoteResultBundleServiceClient(channel).GetBundleChanges(
            new GetVoteResultBundleChangesRequest
            {
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
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
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);
}
