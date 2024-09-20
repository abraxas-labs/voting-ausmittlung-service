// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
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

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultGetImportChangesTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultGetImportChangesTest(TestApplicationFactory factory)
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

        var contestId = Guid.Parse(ContestMockedData.IdStGallenEvoting);
        var stGallenCcId = CountingCircleMockedData.GuidStGallen;
        var gossauCcId = CountingCircleMockedData.GuidGossau;
        var responseStream = ErfassungElectionAdminClient.GetImportChanges(
            new GetResultImportChangesRequest
            {
                ContestId = contestId.ToString(),
                CountingCircleId = stGallenCcId.ToString(),
            },
            new CallOptions(cancellationToken: callCts.Token));

        var readResponsesTask = responseStream.ResponseStream.ReadNIgnoreCancellation(2, callCts.Token);

        // ensure listener is registered, if it is the first query it can take quite long
        await Task.Delay(TimeSpan.FromSeconds(2), callCts.Token);

        // this should be processed
        await PublishMessage(
            new ResultImportChanged(
                contestId,
                stGallenCcId,
                true));

        // this should be ignored (wrong contest)
        await PublishMessage(
            new ResultImportChanged(
                Guid.Parse(ContestMockedData.IdGossau),
                stGallenCcId,
                true));

        // this should be ignored (wrong counting circle)
        await PublishMessage(
            new ResultImportChanged(
                contestId,
                gossauCcId,
                false));

        var responses = await readResponsesTask;
        responses.Should().HaveCount(1);

        callCts.Cancel();
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var responseStream = new ResultImportService.ResultImportServiceClient(channel).GetImportChanges(
            new GetResultImportChangesRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdStGallen,
            },
            new CallOptions(cancellationToken: cts.Token));

        await responseStream.ResponseStream.ReadNIgnoreCancellation(1, cts.Token);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
