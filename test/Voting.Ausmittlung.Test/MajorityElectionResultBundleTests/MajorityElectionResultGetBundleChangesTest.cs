// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultGetBundleChangesTest : BaseTest<MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient>
{
    public MajorityElectionResultGetBundleChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = MonitoringElectionAdminClient.GetBundleChanges(
            new GetMajorityElectionResultBundleChangesRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        var sgId = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;

        // this should be processed
        await PublishMessage(
            new MajorityElectionBundleChanged(
                Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
                sgId));

        // this should be ignored (no access)
        await PublishMessage(
            new MajorityElectionBundleChanged(
                Guid.Parse(MajorityElectionResultBundleMockedData.IdKircheBundle1),
                MajorityElectionResultMockedData.GuidKircheElectionResultInContestKirche));

        // this should be processed
        await PublishMessage(
            new MajorityElectionBundleChanged(
                Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle2),
                sgId));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(2);
        responses.Count(x => x.Id == MajorityElectionResultBundleMockedData.IdStGallenBundle1).Should().Be(1);
        responses.Count(x => x.Id == MajorityElectionResultBundleMockedData.IdStGallenBundle2).Should().Be(1);

        callCts.Cancel();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel).GetBundleChanges(
            new GetMajorityElectionResultBundleChangesRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.MoveNext();
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);
}
