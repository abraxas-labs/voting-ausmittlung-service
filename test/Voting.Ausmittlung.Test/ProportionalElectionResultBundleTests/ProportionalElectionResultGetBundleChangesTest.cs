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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultGetBundleChangesTest : BaseTest<ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient>
{
    public ProportionalElectionResultGetBundleChangesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldNotifyCaller()
    {
        using var callCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var responseStream = MonitoringElectionAdminClient.GetBundleChanges(
            new GetProportionalElectionResultBundleChangesRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        var gossauId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;

        // this should be processed
        await PublishMessage(
            new ProportionalElectionBundleChanged(
                Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1),
                gossauId));

        // this should be ignored (no access)
        await PublishMessage(
            new ProportionalElectionBundleChanged(
                Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle1),
                ProportionalElectionResultMockedData.GuidUzwilElectionResultInContestUzwil));

        // this should be processed
        await PublishMessage(
            new ProportionalElectionBundleChanged(
                Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle2),
                gossauId));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(2);
        responses.Count(x => x.Id == ProportionalElectionResultBundleMockedData.IdGossauBundle1).Should().Be(1);
        responses.Count(x => x.Id == ProportionalElectionResultBundleMockedData.IdGossauBundle2).Should().Be(1);

        callCts.Cancel();
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var responseStream = MonitoringElectionAdminClient.GetBundleChanges(
                    new GetProportionalElectionResultBundleChangesRequest
                    {
                        ElectionResultId = ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
                    },
                    new CallOptions(cancellationToken: cts.Token));

                await responseStream.ResponseStream.MoveNext();
            },
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel).GetBundleChanges(
            new GetProportionalElectionResultBundleChangesRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.MoveNext();
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);
}
