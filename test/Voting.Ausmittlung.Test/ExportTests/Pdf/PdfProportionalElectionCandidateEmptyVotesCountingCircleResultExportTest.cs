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

public class PdfProportionalElectionCandidateEmptyVotesCountingCircleResultExportTest : PdfExportBaseTest<GenerateResultExportsRequest>
{
    public PdfProportionalElectionCandidateEmptyVotesCountingCircleResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => ErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Kandidaten- Zusatz- und Parteistimmen - Kantonratswahl de.pdf";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
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
                    Key = AusmittlungPdfProportionalElectionTemplates.ListCandidateEmptyVotesCountingCircleProtocol.Key,
                    CountingCircleId = CountingCircleMockedData.GuidUzwil,
                    PoliticalBusinessIds = { Guid.Parse(ProportionalElectionUnionEndResultMockedData.UzwilElectionId) },
                },
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
