// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionCandidateEmptyVotesCountingCircleResultEVotingExportTest : PdfExportBaseTest
{
    public PdfProportionalElectionCandidateEmptyVotesCountingCircleResultEVotingExportTest(
        TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => StGallenErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Proporz_Formular2_KandParteiStimmen_EVoting_Pw SG de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfProportionalElectionTemplates.ListCandidateEmptyVotesCountingCircleEVotingProtocol.Key;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionEndResultMockedData.Seed(RunScoped);
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidUzwil && x.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang),
            x => x.EVoting = true);
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.GuidUzwil.ToString(),
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund),
                    countingCircleId: CountingCircleMockedData.GuidUzwil)
                    .ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
