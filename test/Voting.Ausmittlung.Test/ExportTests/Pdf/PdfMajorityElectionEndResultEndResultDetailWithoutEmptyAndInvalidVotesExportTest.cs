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

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultDetailWithoutEmptyAndInvalidVotesExportTest : PdfExportBaseTest<GenerateResultExportsRequest>
{
    public PdfMajorityElectionEndResultDetailWithoutEmptyAndInvalidVotesExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Majorz_Detailergebnisse_exkl_Majorzw de_20200110.pdf";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);

        await ModifyDbEntities<MajorityElectionResult>(_ => true, x => x.State = CountingCircleResultState.AuditedTentatively);
    }

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestId),
            ResultExportRequests =
            {
                new GenerateResultExportRequest
                {
                    Key = AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
                    PoliticalBusinessIds =
                    {
                        Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
                    },
                },
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
