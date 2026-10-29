// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultExportTests;

public class ResultExportDownloadBundleReviewTest : BaseRestTest
{
    private const string ResultExportDownloadEndpoint = "/api/result_export/bundle_review";
    private static readonly Guid _protocolExportBundId = Guid.NewGuid();
    private static readonly Guid _protocolExportStGallenId = Guid.NewGuid();

    public ResultExportDownloadBundleReviewTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());

        await RunOnDb(async db =>
        {
            db.ProtocolExports.Add(new ProtocolExport
            {
                Id = _protocolExportBundId,
                ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                CountingCircleId = CountingCircleMockedData.Bund.Id,
                FileName = "export.pdf",
                State = ProtocolExportState.Completed,
            });
            db.ProtocolExports.Add(new ProtocolExport
            {
                Id = _protocolExportStGallenId,
                ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
                CountingCircleId = CountingCircleMockedData.StGallen.Id,
                FileName = "export.pdf",
                State = ProtocolExportState.Completed,
            });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task ExportShouldWork()
    {
        var request = new FetchBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            CountingCircleId = CountingCircleMockedData.StGallen.BasisCountingCircleId,
            ProtocolExportId = _protocolExportStGallenId,
        };

        var response = await AssertStatus(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(ResultExportDownloadEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);
    }

    [Fact]
    public async Task ExportShouldNotWorkWrongTenant()
    {
        var request = new FetchBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.Bund.BasisCountingCircleId,
            ProtocolExportId = _protocolExportBundId,
        };

        await AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(ResultExportDownloadEndpoint, request),
            HttpStatusCode.Forbidden,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
    }

    [Fact]
    public async Task ExportShouldNotWorkDifferentCountingCircle()
    {
        var request = new FetchBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            CountingCircleId = CountingCircleMockedData.StGallen.BasisCountingCircleId,
            ProtocolExportId = _protocolExportBundId,
        };

        await AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(ResultExportDownloadEndpoint, request),
            HttpStatusCode.BadRequest,
            "Bundle protocol exports contain invalid counting circle ids");
    }

    [Fact]
    public async Task ExportShouldNotWorkDifferentContest()
    {
        var request = new FetchBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            CountingCircleId = CountingCircleMockedData.StGallen.BasisCountingCircleId,
            ProtocolExportId = _protocolExportBundId,
        };

        await AssertProblemDetails(
            () => ErfassungElectionAdminClient.PostAsJsonAsync(ResultExportDownloadEndpoint, request),
            HttpStatusCode.BadRequest,
            "Couldn't find all protocol exports");
    }

    protected override HttpClient CreateHttpClient(params string[] roles)
        => CreateHttpClient(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultExportDownloadEndpoint, new FetchBundleReviewExportRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            CountingCircleId = CountingCircleMockedData.StGallen.BasisCountingCircleId,
            ProtocolExportId = _protocolExportStGallenId,
        });
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungRestrictedBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
