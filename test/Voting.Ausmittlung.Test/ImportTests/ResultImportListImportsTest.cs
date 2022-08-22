// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Xunit;

namespace Voting.Ausmittlung.Test.ImportTests;

public class ResultImportListImportsTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultImportListImportsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ResultImportMockedData.Seed(RunScoped);
        await PermissionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        var imports = await MonitoringElectionAdminClient.ListImportsAsync(NewValidRequest());
        imports.ShouldMatchSnapshot();
    }

    [Fact]
    public Task ShouldThrowOtherContest()
    {
        return AssertStatus(
            async
                () => await MonitoringElectionAdminClient.ListImportsAsync(new ListResultImportsRequest
                {
                    ContestId = ContestMockedData.IdGossau,
                }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .ListImportsAsync(NewValidRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(
        bool authorize = true,
        string? tenant = "default-tenant-id",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateGrpcChannel(authorize, SecureConnectTestDefaults.MockedTenantUzwil.Id, userId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ListResultImportsRequest NewValidRequest()
    {
        return new ListResultImportsRequest { ContestId = ContestMockedData.IdUzwilEvoting };
    }
}
