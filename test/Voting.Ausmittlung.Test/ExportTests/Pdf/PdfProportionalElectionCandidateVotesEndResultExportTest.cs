// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionCandidateVotesEndResultExportTest : PdfProportionalElectionExportBaseTest
{
    public PdfProportionalElectionCandidateVotesEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        tenantId: SecureConnectTestDefaults.MockedTenantUzwil.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Proporz_FormularB_KandStimmen_Kantonratswahl de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.ListCandidateVotesEndResults.Key;

    [Fact]
    public async Task TestPdfInCantonZhShouldHaveCandidateNumberOrdering()
    {
        await ModifyDbEntities<DomainOfInfluence>(
            doi => doi.SnapshotContestId == ContestMockedData.GuidBundesurnengang,
            doi => doi.Canton = DomainOfInfluenceCanton.Zh);

        await TestPdfReport("_zh");
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var uzwilElectionId = Guid.Parse(ProportionalElectionUnionEndResultMockedData.UzwilElectionId);

            var candidates = db.ProportionalElectionCandidates
                .AsTracking()
                .Where(c => c.ProportionalElectionList.ProportionalElectionId == uzwilElectionId && c.ProportionalElectionList.OrderNumber == "2");

            // swap positions to test whether canton related sorting works.
            var candidate1 = candidates.Single(x => x.Position == 1);
            var candidate2 = candidates.Single(x => x.Position == 2);

            candidate1.Number = "02";
            candidate1.Position = 2;
            candidate2.Number = "01";
            candidate2.Position = 1;

            await db.SaveChangesAsync();
        });
    }

    protected override Task<bool> SetToSubmissionOngoing()
    {
        return SetToSubmissionOngoing(Guid.Parse(ProportionalElectionUnionEndResultMockedData.UzwilElectionId));
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
                    SecureConnectTestDefaults.MockedTenantUzwil.Id,
                    politicalBusinessId: Guid.Parse(ProportionalElectionUnionEndResultMockedData.UzwilElectionId))
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
