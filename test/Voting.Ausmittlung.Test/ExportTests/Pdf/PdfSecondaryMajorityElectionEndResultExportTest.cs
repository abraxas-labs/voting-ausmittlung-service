// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfSecondaryMajorityElectionEndResultExportTest : PdfMajorityElectionEndResultExportBaseTest
{
    public PdfSecondaryMajorityElectionEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Majorz_Wahlprotokoll_short2 de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfSecondaryMajorityElectionTemplates.EndResultProtocol.Key;

    [Fact]
    public async Task TestPdfWithSingleCountingCircle()
    {
        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.TotalCountOfCountingCircles = 1);
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.CountingMachine = CountingMachine.CalibratedScales);

        var request = NewRequest();
        await TestPdfReport("_with_single_counting_circle", TestClient, request);
    }

    protected override StartProtocolExportsRequest NewRequest()
    {
        return new StartProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdBundesurnengang,
            ExportTemplateIds =
            {
                AusmittlungUuidV5.BuildExportTemplate(
                    TemplateKey,
                    SecureConnectTestDefaults.MockedTenantStGallen.Id,
                    politicalBusinessId: Guid.Parse(MajorityElectionEndResultMockedData.SecondaryElectionId2))
                    .ToString(),
            },
        };
    }
}
