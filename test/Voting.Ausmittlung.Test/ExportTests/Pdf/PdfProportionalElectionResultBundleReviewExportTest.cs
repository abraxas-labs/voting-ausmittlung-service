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
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfProportionalElectionResultBundleReviewExportTest : PdfBundleReviewExportBaseTest
{
    public PdfProportionalElectionResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
        roles: RolesMockedData.ErfassungElectionAdmin);

    protected override string NewRequestExpectedFileName => "Bundkontrolle 2.pdf";

    [Fact]
    public async Task TestBundleWithoutParty()
    {
        var request = NewRequest();
        request.PoliticalBusinessResultBundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle1);
        await TestPdfReport("_without_party", request, "Bundkontrolle 1.pdf");
    }

    protected override async Task SeedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await ProportionalElectionResultBallotMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var bundles = await db.ProportionalElectionBundles.AsTracking().ToListAsync();
            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            var ballots = await db.ProportionalElectionResultBallots.AsTracking().ToListAsync();
            foreach (var ballot in ballots)
            {
                ballot.MarkedForReview = true;
            }

            await db.SaveChangesAsync();
        });
    }

    protected override HttpClient CreateHttpClient(params string[] roles)
        => CreateHttpClient(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override GenerateResultBundleReviewExportRequest NewRequest()
    {
        return new GenerateResultBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            TemplateKey = AusmittlungPdfProportionalElectionTemplates.ResultBundleReview.Key,
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            PoliticalBusinessResultBundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdUzwilBundle2),
            PoliticalBusinessId = Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds),
        };
    }
}
