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

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultGetBallotTest : MajorityElectionResultBundleBaseTest
{
    public MajorityElectionResultGetBallotTest(TestApplicationFactory factory)
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
        await CreateBallot(MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetBallotAsync(NewValidRequest(req =>
            {
                req.BallotNumber = LatestBallotNumber;
                req.BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3;
            })),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldReturnAsErfassungCreatorOtherThanBundleCreatorIfReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview, MajorityElectionResultBundleMockedData.StGallenBundle3.Id);
        var ballot = await ErfassungCreatorClient.GetBallotAsync(NewValidRequest(req =>
        {
            req.BallotNumber = LatestBallotNumber;
            req.BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle3;
        }));
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
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
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

    private GetMajorityElectionResultBallotRequest NewValidRequest(
        Action<GetMajorityElectionResultBallotRequest>? customizer = null)
    {
        var req = new GetMajorityElectionResultBallotRequest
        {
            BallotNumber = 1,
            BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
        };
        customizer?.Invoke(req);
        return req;
    }
}
