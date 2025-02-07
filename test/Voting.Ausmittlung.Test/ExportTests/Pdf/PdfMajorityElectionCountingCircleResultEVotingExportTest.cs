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

public class PdfMajorityElectionCountingCircleResultEVotingExportTest : PdfExportBaseTest
{
    public PdfMajorityElectionCountingCircleResultEVotingExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => StGallenErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Majorz_Gemeindeprotokoll_EVoting_Mw SG de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfMajorityElectionTemplates.CountingCircleEVotingProtocol.Key;

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionResultMockedData.InjectCandidateResults(RunScoped);
        await ModifyDbEntities<CountingCircle>(
            x => x.BasisCountingCircleId == CountingCircleMockedData.GuidStGallen,
            x => x.EVoting = true);
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidStGallen && x.ContestId == Guid.Parse(ContestMockedData.IdBundesurnengang),
            x => x.EVoting = true);
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            CountingCircleId = CountingCircleMockedData.GuidStGallen.ToString(),
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: MajorityElectionMockedData.StGallenMajorityElectionInContestBund.Id,
                    countingCircleId: CountingCircleMockedData.GuidStGallen)
                    .ToString(),
            },
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
