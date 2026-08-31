// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfProportionalElectionUnionDoubleProportionalResultExportBaseTest : PdfExportBaseTest
{
    protected PdfProportionalElectionUnionDoubleProportionalResultExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => MonitoringElectionAdminClient;

    [Fact]
    public virtual async Task TestPdfWithFormattedValues()
    {
        using var d = EnableFormattedTemplateValues(DomainOfInfluenceCanton.Zh);
        await TestPdfReport("_formatted-values");
    }

    protected override async Task SeedData()
    {
        ResetDb();
        await ZhMockedData.Seed(RunScoped, true);
    }

    protected override Task<bool> SetToSubmissionOngoing()
        => Task.FromResult(false); // These reports only make sense when the submission is finished.

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
