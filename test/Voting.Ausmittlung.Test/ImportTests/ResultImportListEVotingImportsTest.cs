// (c) Copyright by Abraxas Informatik AG
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

public class ResultImportListEVotingImportsTest : BaseTest<ResultImportService.ResultImportServiceClient>
{
    public ResultImportListEVotingImportsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ResultImportEVotingMockedData.Seed(RunScoped);
        await PermissionMockedData.Seed(RunScoped);
    }

    [Fact]
    public async Task ShouldWork()
    {
        var imports = await MonitoringElectionAdminClient.ListEVotingImportsAsync(NewValidRequest());
        imports.ShouldMatchSnapshot();
    }

    [Fact]
    public Task ShouldThrowOtherContest()
    {
        return AssertStatus(
            async
                () => await MonitoringElectionAdminClient.ListEVotingImportsAsync(new ListEVotingResultImportsRequest
                {
                    ContestId = ContestMockedData.IdGossau,
                }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultImportService.ResultImportServiceClient(channel)
            .ListEVotingImportsAsync(NewValidRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateGrpcChannel(authorize, SecureConnectTestDefaults.MockedTenantUzwil.Id, userId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private ListEVotingResultImportsRequest NewValidRequest()
    {
        return new ListEVotingResultImportsRequest { ContestId = ContestMockedData.IdUzwilEVoting };
    }
}
