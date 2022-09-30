// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionEndResultCalculationExportTest : PdfExportBaseTest<GenerateResultExportsRequest>
{
    public PdfProportionalElectionEndResultCalculationExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => MonitoringElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Verteilung der Sitze - Nationalratswahl de.pdf";

    protected override Task SeedData() => ProportionalElectionEndResultSgExampleMockedData.Seed(RunScoped);

    protected override GenerateResultExportsRequest NewRequest()
    {
        return new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                       {
                           new GenerateResultExportRequest
                           {
                               Key = AusmittlungPdfProportionalElectionTemplates.EndResultCalculation.Key,
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
