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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionDoubleProportionalResultSuperApportionmentGetAvailableLotDecisionsTest : ProportionalElectionDoubleProportionalResultBaseTest
{
    public ProportionalElectionDoubleProportionalResultSuperApportionmentGetAvailableLotDecisionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await CreateDpResult(ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot);
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        var availableLotDecisions = await MonitoringElectionAdminClient.GetDoubleProportionalResultSuperApportionmentAvailableLotDecisionsAsync(NewValidRequest());
        availableLotDecisions.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowForeignMonitoringElection()
    {
        await AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.GetDoubleProportionalResultSuperApportionmentAvailableLotDecisionsAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task NoDpResultShouldThrow()
    {
        await DeleteDpResult(ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetDoubleProportionalResultSuperApportionmentAvailableLotDecisionsAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetDoubleProportionalResultSuperApportionmentAvailableLotDecisionsAsync(NewValidRequest());
    }

    private GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest NewValidRequest(
        Action<GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetProportionalElectionDoubleProportionalResultSuperApportionmentAvailableLotDecisionsRequest()
        {
            ProportionalElectionId = ZhMockedData.ProportionalElectionGuidSingleDoiSuperLot.ToString(),
        };

        action?.Invoke(request);
        return request;
    }
}
