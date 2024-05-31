// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
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
    public async Task TestAsErfassungCreatorWithoutBundleControlShouldWork()
    {
        var response = await ErfassungCreatorWithoutBundleControlClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsErfassungBundleControllerShouldWork()
    {
        var response = await ErfassungBundleControllerClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsErfassungElectionSupporterShouldWork()
    {
        var response = await ErfassungElectionSupporterClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsErfassungElectionAdminShouldWork()
    {
        var response = await ErfassungElectionAdminClient.ListAsync(new Empty());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringElectionSupporterShouldWork()
    {
        var response = await MonitoringElectionSupporterClient.ListAsync(new Empty());
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

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
        yield return RolesMockedData.ReportExporterApi;
    }
}
