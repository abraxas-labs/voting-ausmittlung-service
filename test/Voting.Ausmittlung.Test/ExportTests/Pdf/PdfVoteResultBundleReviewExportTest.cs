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

public class PdfVoteResultBundleReviewExportTest : PdfBundleReviewExportBaseTest
{
    public PdfVoteResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantGossau.Id,
        roles: RolesMockedData.ErfassungElectionAdmin);

    protected override string NewRequestExpectedFileName => "Bundkontrolle 1.pdf";

    protected override async Task SeedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteEndResultMockedData.Seed(RunScoped);
        await VoteResultBundleMockedData.Seed(RunScoped);
        await VoteResultBallotMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var bundles = await db.VoteResultBundles.AsTracking().ToListAsync();
            foreach (var bundle in bundles)
            {
                bundle.State = BallotBundleState.Reviewed;
            }

            var ballots = await db.VoteResultBallots.AsTracking().ToListAsync();
            foreach (var ballot in ballots)
            {
                ballot.MarkedForReview = true;
            }

            await db.SaveChangesAsync();
        });
    }

    protected override HttpClient CreateHttpClient(params string[] roles)
        => CreateHttpClient(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    protected override GenerateResultBundleReviewExportRequest NewRequest()
    {
        return new GenerateResultBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
            PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
        };
    }
}
