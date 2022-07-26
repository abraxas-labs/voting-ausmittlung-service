﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public class PdfMajorityElectionResultBundleReviewExportTest : PdfExportBaseTest<GenerateResultBundleReviewExportRequest>
{
    public PdfMajorityElectionResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override HttpClient TestClient => CreateHttpClient(
        tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
        roles: RolesMockedData.ErfassungElectionAdmin);

    public override string ExportEndpoint => $"{ResultExportEndpoint}/bundle_review";

    protected override string NewRequestExpectedFileName => "Bundkontrolle 1.pdf";

    protected override string ContestId => ContestMockedData.IdBundesurnengang;

    public override Task TestPdfAfterTestingPhaseEnded()
    {
        // Cannot test this report, as all bundles are deleted after the testing phase ends
        return Task.CompletedTask;
    }

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

    protected override GenerateResultBundleReviewExportRequest NewRequest()
    {
        return new GenerateResultBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestId),
            TemplateKey = AusmittlungPdfMajorityElectionTemplates.ResultBundleReview.Key,
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            PoliticalBusinessResultBundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
            PoliticalBusinessId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
        };
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
