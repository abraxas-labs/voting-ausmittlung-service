// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionDoubleProportionalResultExportTest : PdfExportBaseTest
{
    public PdfProportionalElectionDoubleProportionalResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        tenantId: SecureConnectTestDefaults.MockedTenantStGallen.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "DPT3_DopP_Sitzverteilung.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.EndResultDoubleProportional.Key;

    protected override async Task SeedData()
    {
        await ProportionalElectionEndResultSgExampleMockedData.Seed(RunScoped);

        await ModifyDbEntities<ProportionalElection>(
            pe => pe.Id == ProportionalElectionEndResultSgExampleMockedData.GuidStGallenNationalratElection,
            pe => pe.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        await RunScoped<DoubleProportionalResultBuilder>(async builder =>
            await builder.BuildForElection(ProportionalElectionEndResultSgExampleMockedData.GuidStGallenNationalratElection));
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                        TemplateKey,
                        SecureConnectTestDefaults.MockedTenantStGallen.Id,
                        politicalBusinessId: ProportionalElectionEndResultSgExampleMockedData.GuidStGallenNationalratElection)
                    .ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
