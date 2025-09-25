// (c) Copyright by Abraxas Informatik AG
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

public class WabstiCWPStaticGemeindenEinzelExportTest : CsvExportBaseTest
{
    public WabstiCWPStaticGemeindenEinzelExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "WPStatic_Gemeinden_St_Gallen.csv";

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ModifyDbEntities<ProportionalElectionResult>(
            _ => true,
            x => x.State = CountingCircleResultState.SubmissionDone);
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        await ModifyDbEntities<ProportionalElectionResult>(
            _ => true,
            x => x.State = CountingCircleResultState.SubmissionOngoing);
        return true;
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ExportTemplateIds = new List<Guid>
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungWabstiCTemplates.WPStaticGemeindenEinzel.Key,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund)),
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
