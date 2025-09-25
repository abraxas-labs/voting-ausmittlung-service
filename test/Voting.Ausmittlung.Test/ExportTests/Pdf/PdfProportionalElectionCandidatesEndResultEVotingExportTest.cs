// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionCandidatesEndResultEVotingExportTest : PdfProportionalElectionExportBaseTest
{
    public PdfProportionalElectionCandidatesEndResultEVotingExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => CreateService(
        SecureConnectTestDefaults.MockedTenantStGallen.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Proporz_Formular5b_KandParteiErgebnisse_EVoting_Pw SG de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.ListCandidateEndResultsEVoting.Key;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdBundesurnengang),
            x => x.EVoting = true);
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
    }

    protected override Task<bool> SetToSubmissionOngoing()
    {
        return SetToSubmissionOngoing(Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund));
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
                    politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund))
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
