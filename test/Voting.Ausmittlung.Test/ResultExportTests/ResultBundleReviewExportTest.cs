// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultExportTests;

public class ResultBundleReviewExportTest : BaseRestTest
{
    private const string ResultBundleReviewExportEndpoint = "/api/result_export/bundle_review";

    public ResultBundleReviewExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await VoteResultBundleMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ShouldThrowUnknownKey()
    {
        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(
                ResultBundleReviewExportEndpoint,
                new GenerateResultBundleReviewExportRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    TemplateKey = "unknown",
                    CountingCircleId = CountingCircleMockedData.GuidGossau,
                    PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
                    PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
                }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldThrowCountingCircleWithoutPermission()
    {
        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(
                ResultBundleReviewExportEndpoint,
                new GenerateResultBundleReviewExportRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
                    CountingCircleId = CountingCircleMockedData.GuidStGallen,
                    PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
                    PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
                }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldThrowNoPoliticalBusinessResultBundleId()
    {
        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(
                ResultBundleReviewExportEndpoint,
                new GenerateResultBundleReviewExportRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
                    CountingCircleId = CountingCircleMockedData.GuidGossau,
                    PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldThrowNoValidPoliticalBusinessId()
    {
        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(
                ResultBundleReviewExportEndpoint,
                new GenerateResultBundleReviewExportRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
                    CountingCircleId = CountingCircleMockedData.GuidGossau,
                    PoliticalBusinessId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestBundWithoutChilds),
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldThrowContestLocked()
    {
        await RunOnDb(async db =>
        {
            var contest = await db.Contests.AsTracking().Where(x => x.Id == Guid.Parse(ContestMockedData.IdBundesurnengang)).FirstAsync();
            contest.State = ContestState.PastLocked;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(
                ResultBundleReviewExportEndpoint,
                new GenerateResultBundleReviewExportRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
                    CountingCircleId = CountingCircleMockedData.GuidGossau,
                    PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
                    PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldThrowPoliticalBusinessNotActive()
    {
        await RunOnDb(async db =>
        {
            var vote = await db.Votes.AsTracking().Where(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen)).FirstAsync();
            vote.Active = false;
            await db.SaveChangesAsync();
        });

        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(
                ResultBundleReviewExportEndpoint,
                new GenerateResultBundleReviewExportRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
                    CountingCircleId = CountingCircleMockedData.GuidGossau,
                    PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
                    PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BundleReviewExportShouldCreateEvent()
    {
        var request = new GenerateResultBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
            PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
        };

        await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(ResultBundleReviewExportEndpoint, request),
            HttpStatusCode.OK);

        var events = EventPublisherMock.GetPublishedEvents<BundleReviewExportGenerated>().ToList();
        events.MatchSnapshot();
    }

    protected override HttpClient CreateHttpClient(
        bool authorize = true,
        string? tenant = "000000000000000000",
        string? userId = "default-user-id",
        params string[] roles)
        => base.CreateHttpClient(authorize, SecureConnectTestDefaults.MockedTenantGossau.Id, userId, roles);

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultBundleReviewExportEndpoint, new GenerateResultBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            TemplateKey = AusmittlungPdfVoteTemplates.ResultBundleReview.Key,
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            PoliticalBusinessResultBundleId = Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1),
            PoliticalBusinessId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
        });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
