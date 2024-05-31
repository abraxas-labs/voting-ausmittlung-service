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

public class VoteResultGetTest : BaseTest<VoteResultService.VoteResultServiceClient>
{
    private const string IdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";

    public VoteResultGetTest(TestApplicationFactory factory)
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
        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithResultId()
    {
        var response = await ErfassungCreatorClient.GetAsync(new GetVoteResultRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithResultId()
    {
        var response = await ErfassungElectionAdminClient.GetAsync(new GetVoteResultRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminWithResultId()
    {
        var response = await MonitoringElectionAdminClient.GetAsync(new GetVoteResultRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(NewValidRequest(r => r.VoteId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetVoteResultRequest
            {
                VoteId = VoteMockedData.IdUzwilVoteInContestBundWithoutChilds,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .GetAsync(NewValidRequest());
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

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetVoteResultRequest NewValidRequest(Action<GetVoteResultRequest>? customizer = null)
    {
        var r = new GetVoteResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        };
        customizer?.Invoke(r);
        return r;
    }
}
