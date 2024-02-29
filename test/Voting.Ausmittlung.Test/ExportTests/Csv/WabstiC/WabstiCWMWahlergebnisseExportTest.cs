// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC;

public class WabstiCWMWahlergebnisseExportTest : CsvExportBaseTest
{
    public WabstiCWMWahlergebnisseExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "csv_Export_Detailergebnisse_Majorzw de_20290212.csv";

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<MajorityElectionResult>(
            x => x.Id == MajorityElectionEndResultMockedData.StGallenResultGuid,
            x => x.State = CountingCircleResultState.SubmissionOngoing);
    }

    protected override Task SeedData() => MajorityElectionEndResultMockedData.Seed(RunScoped);

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungWabstiCTemplates.WMWahlergebnisse.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: MajorityElectionEndResultMockedData.ElectionGuid),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.ErfassungCreator;
    }
}
