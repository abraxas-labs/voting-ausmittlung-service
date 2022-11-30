// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfVoteEndResultExportTest : PdfExportBaseTest<GenerateResultExportsRequest>
{
    public PdfVoteEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Gesamtergebnis aller Sachgeschäfte CT.pdf";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);
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
                    Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                    DomainOfInfluenceType = DomainOfInfluenceType.Ct,
                    PoliticalBusinessIds =
                    {
                        VoteEndResultMockedData.VoteGuid,
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
