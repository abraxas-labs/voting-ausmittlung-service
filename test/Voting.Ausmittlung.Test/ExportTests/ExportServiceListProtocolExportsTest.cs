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
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportServiceListProtocolExportsTest : BaseTest<ExportService.ExportServiceClient>
{
    public ExportServiceListProtocolExportsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ShouldWorkAsAllRolesAndReturnTheSame()
    {
        var roleClientsToTest = new[]
        {
            MonitoringElectionAdminClient,
            ErfassungElectionAdminClient,
            ErfassungCreatorClient,
        };

        foreach (var client in roleClientsToTest)
        {
            var response = await client.ListProtocolExportsAsync(NewValidRequest());
            response.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringElectionAdminWithoutCountingCircleId()
    {
        var response = await MonitoringElectionAdminClient.ListProtocolExportsAsync(NewValidRequest(x => x.CountingCircleId = string.Empty));
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowAsErfassungElectionAdminWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ListProtocolExportsAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowAsErfassungCreatorWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.ListProtocolExportsAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ListProtocolExportsAsync(new() { ContestId = ContestMockedData.IdGossau }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .ListProtocolExportsAsync(NewValidRequest());
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

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private ListProtocolExportsRequest NewValidRequest(Action<ListProtocolExportsRequest>? reqCustomizer = null)
    {
        var req = new ListProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        };
        reqCustomizer?.Invoke(req);
        return req;
    }
}
