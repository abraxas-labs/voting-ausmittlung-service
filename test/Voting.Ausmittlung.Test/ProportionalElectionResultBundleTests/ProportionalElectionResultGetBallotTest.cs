// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
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
        var response = await BundleErfassungElectionAdminClient.GetBallotAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnAsErfassungCreator()
    {
        var response = await BundleErfassungElectionAdminClient.GetBallotAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await BundleMonitoringElectionAdminClient.GetBallotAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowAsErfassungCreatorOtherThanBundleCreator()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.GetBallotAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldReturnAsErfassungCreatorOtherThanBundleCreatorIfReview()
    {
        await RunBundleToState(BallotBundleState.ReadyForReview);
        var ballot = await BundleErfassungCreatorClientSecondUser.GetBallotAsync(NewValidRequest());
        ballot.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await BundleErfassungCreatorClientSecondUser.GetBallotAsync(NewValidRequest(x => x.BallotNumber = 10)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .GetBallotAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
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
