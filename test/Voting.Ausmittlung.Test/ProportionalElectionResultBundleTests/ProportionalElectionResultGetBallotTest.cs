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
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultGetBallotTest : ProportionalElectionResultBundleBaseTest
{
    public ProportionalElectionResultGetBallotTest(TestApplicationFactory factory)
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
    public async Task ShouldThrowAsErfassungCreatorOtherThanBundleCreator()
    {
        await CreateBallot(ProportionalElectionResultBundleMockedData.GossauBundle3.Id);
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetBallotAsync(NewValidRequest(req =>
            {
                req.BallotNumber = LatestBallotNumber;
                req.BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle3;
            })),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldReturnAsErfassungCreatorOtherThanBundleCreatorIfReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, ProportionalElectionResultBundleMockedData.GossauBundle3.Id);
        var ballot = await ErfassungCreatorClient.GetBallotAsync(NewValidRequest(req =>
        {
            req.BallotNumber = LatestBallotNumber;
            req.BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle3;
        }));
        ballot.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetBallotAsync(new GetProportionalElectionResultBallotRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle3,
                BallotNumber = 10,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .GetBallotAsync(NewValidRequest());
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

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await CreateBallot();
    }

    private GetProportionalElectionResultBallotRequest NewValidRequest(
        Action<GetProportionalElectionResultBallotRequest>? customizer = null)
    {
        var req = new GetProportionalElectionResultBallotRequest
        {
            BallotNumber = 1,
            BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
        };
        customizer?.Invoke(req);
        return req;
    }
}
