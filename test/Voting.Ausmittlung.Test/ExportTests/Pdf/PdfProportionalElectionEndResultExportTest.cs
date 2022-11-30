// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionEndResultExportTest : PdfExportBaseTest<GenerateResultExportsRequest>
{
    public PdfProportionalElectionEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
        roles: RolesMockedData.MonitoringElectionAdmin);

    protected override string NewRequestExpectedFileName => "Listenergbenisse - Nationalratswahl de.pdf";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    protected override Task SeedData()
    {
        return ProportionalElectionEndResultSgExampleMockedData.Seed(RunScoped);
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
                    Key = AusmittlungPdfProportionalElectionTemplates.ListVotesEndResults.Key,
                    PoliticalBusinessIds = { Guid.Parse(ProportionalElectionEndResultSgExampleMockedData.IdStGallenNationalratElection) },
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
