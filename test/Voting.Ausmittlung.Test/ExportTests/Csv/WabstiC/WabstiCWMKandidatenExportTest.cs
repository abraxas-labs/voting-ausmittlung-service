// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv.WabstiC;

public class WabstiCWMKandidatenExportTest : CsvExportBaseTest
{
    public WabstiCWMKandidatenExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "WM_Kandidaten.csv";

    [Fact]
    public async Task TestCsvWithFinalizedEndResult()
    {
        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElection.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang),
            x => x.Finalized = true);
        await TestCsvSnapshot(NewRequest(), NewRequestExpectedFileName, "finalized");
    }

    protected override Task SeedData()
    {
        return MajorityElectionEndResultMockedData.Seed(RunScoped);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungWabstiCTemplates.WMKandidaten.Key,
                    },
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
