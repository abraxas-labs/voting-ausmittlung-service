// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionUnionListVotesEndResultsExportTest : PdfExportBaseTest
{
    public PdfProportionalElectionUnionListVotesEndResultsExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        tenantId: CountingCircleMockedData.Bund.ResponsibleAuthority.SecureConnectId,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Proporz_FormularC_Listenergebnisse_Kantonratswahl_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.ListVotesPoliticalBusinessUnionEndResults.Key;

    [Fact]
    public async Task SameUnionListShortDescriptionShouldCombineLists()
    {
        var unionList = await RunOnDb(db => db.ProportionalElectionUnionLists
            .SingleAsync(x =>
                x.ProportionalElectionUnionId == Guid.Parse(ProportionalElectionUnionEndResultMockedData.UnionId) &&
                x.OrderNumber == "1b"));

        await ModifyDbEntities<ProportionalElectionUnionListTranslation>(
            x => x.ProportionalElectionUnionListId == unionList.Id,
            x => x.ShortDescription = "Liste 1a de");

        await TestPdfReport("_same_short_description");
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
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
                    CountingCircleMockedData.Bund.ResponsibleAuthority.SecureConnectId,
                    politicalBusinessUnionId: ProportionalElectionUnionEndResultMockedData.Union.Id)
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
