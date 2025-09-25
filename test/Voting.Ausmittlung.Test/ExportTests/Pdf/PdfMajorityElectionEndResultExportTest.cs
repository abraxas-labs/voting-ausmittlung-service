// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionEndResultExportTest : PdfMajorityElectionEndResultExportBaseTest
{
    public PdfMajorityElectionEndResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string NewRequestExpectedFileName => "Majorz_Wahlprotokoll_Majorzw de_20290212.pdf";

    protected override string TemplateKey => AusmittlungPdfMajorityElectionTemplates.EndResultProtocol.Key;

    [Fact]
    public async Task TestPdfWithSingleCountingCircle()
    {
        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.TotalCountOfCountingCircles = 1);
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.CountingMachine = CountingMachine.CalibratedScales);
        await RunOnDb(async db =>
        {
            await db.MajorityElectionResults
                .Where(x => x.MajorityElection.ContestId == ContestMockedData.GuidBundesurnengang
                    && x.CountingCircle.BasisCountingCircleId != CountingCircleMockedData.GuidUzwil)
                .ExecuteDeleteAsync();
        });

        var request = NewRequest();
        await TestPdfReport("_with_single_counting_circle", TestClient, request);
    }

    [Fact]
    public async Task TestPdfWithSingleCountingCircleSubmissionOngoing()
    {
        await SetToSubmissionOngoing();

        await ModifyDbEntities<MajorityElectionEndResult>(
            x => x.MajorityElectionId == Guid.Parse(MajorityElectionEndResultMockedData.ElectionId),
            x => x.TotalCountOfCountingCircles = 1);
        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.ContestId == ContestMockedData.GuidBundesurnengang,
            x => x.CountingMachine = CountingMachine.CalibratedScales);
        await RunOnDb(async db =>
        {
            await db.MajorityElectionResults
                .Where(x => x.MajorityElection.ContestId == ContestMockedData.GuidBundesurnengang
                    && x.CountingCircle.BasisCountingCircleId != CountingCircleMockedData.GuidUzwil)
                .ExecuteDeleteAsync();
        });

        var request = NewRequest();
        await TestPdfReport("_with_single_counting_circle_submission-ongoing", TestClient, request);
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
                    politicalBusinessId: Guid.Parse(MajorityElectionEndResultMockedData.ElectionId))
                    .ToString(),
            },
        };
    }
}
