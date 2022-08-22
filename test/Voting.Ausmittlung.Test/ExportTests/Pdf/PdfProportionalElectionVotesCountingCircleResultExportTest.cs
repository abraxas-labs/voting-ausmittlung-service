// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionVotesCountingCircleResultExportTest : PdfExportBaseTest
{
    public PdfProportionalElectionVotesCountingCircleResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => ErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Wahlzettelreport - Nationalratswahl de.pdf";

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
                               Key = AusmittlungPdfProportionalElectionTemplates.ListVotesCountingCircleProtocol.Key,
                               CountingCircleId = CountingCircleMockedData.GuidStGallen,
                               PoliticalBusinessIds = { Guid.Parse(ProportionalElectionEndResultSgExampleMockedData.IdStGallenNationalratElection) },
                           },
                       },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
