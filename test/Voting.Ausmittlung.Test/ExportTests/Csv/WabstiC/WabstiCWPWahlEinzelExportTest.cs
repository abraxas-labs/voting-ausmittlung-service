// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC;

public class WabstiCWPWahlEinzelExportTest : CsvExportBaseTest
{
    public WabstiCWPWahlEinzelExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => BundMonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "WP_Wahl_Bund.csv";

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
    }

    protected override Task<bool> SetToSubmissionOngoing()
        => Task.FromResult(false); // Only includes data from end result

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungWabstiCTemplates.WPWahlEinzel.Key,
                    CountingCircleMockedData.Bund.ResponsibleAuthority.SecureConnectId,
                    politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestBund)),
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
