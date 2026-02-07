// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultGetBallotTest : VoteResultBundleBaseTest
{
    public VoteResultGetBallotTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetBallotAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetBallotAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetBallotAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnAsErfassungCreatorOtherThanBundleCreatorIfReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, VoteResultBundleMockedData.GossauBundle3.Id);
        var ballot = await ErfassungCreatorClient.GetBallotAsync(NewValidRequest(req => req.BundleId = VoteResultBundleMockedData.IdGossauBundle3));
        ballot.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetBallotAsync(NewValidRequest(x => x.BallotNumber = 10)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .GetBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungRestrictedBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await CreateBallot();
    }

    private GetVoteResultBallotRequest NewValidRequest(
        Action<GetVoteResultBallotRequest>? customizer = null)
    {
        var req = new GetVoteResultBallotRequest
        {
            BallotNumber = 1,
            BundleId = VoteResultBundleMockedData.IdGossauBundle1,
        };
        customizer?.Invoke(req);
        return req;
    }
}
