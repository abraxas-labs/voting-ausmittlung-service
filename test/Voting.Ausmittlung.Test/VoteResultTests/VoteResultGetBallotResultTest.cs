// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultGetBallotResultTest : BaseTest<VoteResultService.VoteResultServiceClient>
{
    private const string IdNotFound = "86905eae-8137-45ce-9ff1-6c040c264e0c";

    public VoteResultGetBallotResultTest(TestApplicationFactory factory)
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
    public async Task TestShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetBallotResultAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetBallotResultAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetBallotResultAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetBallotResultAsync(NewValidRequest(r => r.BallotResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetBallotResultAsync(new GetBallotResultRequest()
            {
                BallotResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenBallotResult,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .GetBallotResultAsync(NewValidRequest());
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

    private GetBallotResultRequest NewValidRequest(Action<GetBallotResultRequest>? customizer = null)
    {
        var r = new GetBallotResultRequest
        {
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
        };
        customizer?.Invoke(r);
        return r;
    }
}
