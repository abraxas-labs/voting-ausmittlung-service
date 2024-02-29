// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.PermissionTests;

public class ListPermissionsTest : BaseTest<PermissionService.PermissionServiceClient>
{
    public ListPermissionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestAsErfassungCreatorShouldWork()
    {
        var response = await ErfassungCreatorClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsErfassungElectionAdminShouldWork()
    {
        var response = await ErfassungElectionAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringElectionAdminShouldWork()
    {
        var response = await MonitoringElectionAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new PermissionService.PermissionServiceClient(channel)
            .ListAsync(new Empty());
    }
}
