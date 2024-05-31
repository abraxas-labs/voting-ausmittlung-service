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

public class ProportionalElectionUnionEndResultGetTest : ProportionalElectionUnionResultBaseTest
{
    public ProportionalElectionUnionEndResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowForeignMonitoringElection()
    {
        await AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionUnionResultService.ProportionalElectionUnionResultServiceClient(channel)
            .GetEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private GetProportionalElectionUnionEndResultRequest NewValidRequest()
    {
        return new() { ProportionalElectionUnionId = ZhMockedData.ProportionalElectionUnionIdKtrat };
    }
}
