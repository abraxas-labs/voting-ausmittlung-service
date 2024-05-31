// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

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

namespace Voting.Ausmittlung.Test.ProportionalElectionUnionResultTests;

public class ProportionalElectionUnionDpResultGetTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionDpResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await FinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidKtrat);
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        var endResult = await MonitoringElectionAdminClient.GetDoubleProportionalResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowForeignMonitoringElection()
    {
        await AssertStatus(
        async () => await StGallenMonitoringElectionAdminClient.GetDoubleProportionalResultAsync(NewValidRequest()),
        StatusCode.NotFound);
    }

    [Fact]
    public async Task NotFinalizedShouldThrow()
    {
        await RevertFinalizeUnion(ZhMockedData.ProportionalElectionUnionGuidKtrat);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetDoubleProportionalResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .GetDoubleProportionalResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private GetProportionalElectionUnionDoubleProportionalResultRequest NewValidRequest()
    {
        return new() { ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat };
    }
}
