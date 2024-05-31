// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionResultBundleReviewExportTest : PdfBundleReviewExportBaseTest
{
    public PdfMajorityElectionResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
        roles: RolesMockedData.ErfassungElectionAdmin);

    protected override string NewRequestExpectedFileName => "Bundkontrolle 1.pdf";

    protected override async Task SeedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionEndResultMockedData.Seed(RunScoped);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await MajorityElectionResultBallotMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var bundles = await db.MajorityElectionResultBundles.AsTracking().ToListAsync();
            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            var ballots = await db.MajorityElectionResultBallots.AsTracking().ToListAsync();
            foreach (var ballot in ballots)
            {
                ballot.MarkedForReview = true;
            }

            await db.SaveChangesAsync();
        });
    }

    protected override HttpClient CreateHttpClient(params string[] roles)
        => CreateHttpClient(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override GenerateResultBundleReviewExportRequest NewRequest()
    {
        return new GenerateResultBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            TemplateKey = AusmittlungPdfMajorityElectionTemplates.ResultBundleReview.Key,
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            PoliticalBusinessResultBundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
            PoliticalBusinessId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
        };
    }
}
