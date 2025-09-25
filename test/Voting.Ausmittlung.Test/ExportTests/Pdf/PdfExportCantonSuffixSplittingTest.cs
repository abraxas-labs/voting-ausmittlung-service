// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfExportCantonSuffixSplittingTest : PdfExportBaseTest
{
    public PdfExportCantonSuffixSplittingTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override ExportService.ExportServiceClient TestClient => StGallenErfassungElectionAdminClient;

    protected override string NewRequestExpectedFileName => "Majorz_Gemeindeprotokoll_Mw SG de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfMajorityElectionTemplates.CountingCircleProtocol.Key;

    protected override string SnapshotName => TemplateKey + "_with_canton_suffix_splitting";

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionResultMockedData.InjectCandidateResults(RunScoped);
        var config = GetService<PublisherConfig>();
        config.EnableCantonSuffixTemplateKeys = ["majority_election_counting_circle_protocol"];

        await ModifyDbEntities<MajorityElectionResult>(
            _ => true,
            x => x.State = CountingCircleResultState.SubmissionDone);
    }

    protected override async Task<bool> SetToSubmissionOngoing()
    {
        await ModifyDbEntities<MajorityElectionResult>(
            _ => true,
            x => x.State = CountingCircleResultState.SubmissionOngoing);
        return true;
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
