// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultGetBundlesTest : BaseTest<VoteResultBundleService.VoteResultBundleServiceClient>
{
    private const string IdNotFound = "997f83cb-7ce5-4321-be2e-5e6451d2a3da";

    public VoteResultGetBundlesTest(TestApplicationFactory factory)
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
    public async Task TestShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetBundlesAsync(NewValidRequest());
        ResetBundleIds(response);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetBundlesAsync(NewValidRequest());
        ResetBundleIds(response);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetBundlesAsync(NewValidRequest());
        ResetBundleIds(response);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetBundlesAsync(NewValidRequest(r => r.BallotResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetBundlesAsync(NewValidRequest(r =>
                r.BallotResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenBallotResult)),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .GetBundlesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetVoteResultBundlesRequest NewValidRequest(Action<GetVoteResultBundlesRequest>? customizer = null)
    {
        var r = new GetVoteResultBundlesRequest
        {
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
        };
        customizer?.Invoke(r);
        return r;
    }

    private void ResetBundleIds(ProtoModels.VoteResultBundles response)
    {
        foreach (var bundle in response.Bundles)
        {
            bundle.Id = string.Empty;
        }
    }
}
