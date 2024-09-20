// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public class ProportionalElectionUnionDoubleProportionalResultSubApportionmentGetAvailableLotDecisionsTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionDoubleProportionalResultSubApportionmentGetAvailableLotDecisionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidSubLot);
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        var availableLotDecisions = await MonitoringElectionAdminClient.GetDoubleProportionalResultSubApportionmentAvailableLotDecisionsAsync(NewValidRequest());
        availableLotDecisions.MatchSnapshot();
    }

    [Fact]
    public async Task NoRequiredLotDecisionsShouldWork()
    {
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidKtrat);
        var availableLotDecisions = await MonitoringElectionAdminClient.GetDoubleProportionalResultSubApportionmentAvailableLotDecisionsAsync(
            NewValidRequest(x => x.ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat));
        availableLotDecisions.LotDecisions.Should().BeEmpty();
    }

    [Fact]
    public async Task TestShouldThrowForeignMonitoringElection()
    {
        await AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.GetDoubleProportionalResultSubApportionmentAvailableLotDecisionsAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task NotFinalizedShouldThrow()
    {
        await RevertFinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidSubLot);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetDoubleProportionalResultSubApportionmentAvailableLotDecisionsAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .GetDoubleProportionalResultSubApportionmentAvailableLotDecisionsAsync(NewValidRequest());
    }

    private GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest NewValidRequest(
        Action<GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest>? action = null)
    {
        var request = new GetProportionalElectionUnionDoubleProportionalResultSubApportionmentAvailableLotDecisionsRequest()
        {
            ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdSubLot,
        };

        action?.Invoke(request);
        return request;
    }
}
