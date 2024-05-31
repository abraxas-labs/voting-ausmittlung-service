// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfProportionalElectionUnionDoubleProportionalResultExportBaseTest : PdfExportBaseTest
{
    protected PdfProportionalElectionUnionDoubleProportionalResultExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => MonitoringElectionAdminClient;

    protected override async Task SeedData()
    {
        ResetDb();
        await ZhMockedData.Seed(RunScoped, true);
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
