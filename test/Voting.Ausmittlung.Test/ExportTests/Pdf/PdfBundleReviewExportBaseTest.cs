// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfBundleReviewExportBaseTest : BaseRestTest
{
    private const string ExportEndpoint = "/api/result_export/bundle_review";
    private const string PdfExtension = ".pdf";

    protected PdfBundleReviewExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected virtual HttpClient TestClient => MonitoringElectionAdminClient;

    protected abstract string NewRequestExpectedFileName { get; }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await SeedData();
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public virtual async Task TestPdf()
    {
        var request = NewRequest();
        await TestPdfReport(string.Empty, request, NewRequestExpectedFileName);
    }

    protected async Task TestPdfReport(string snapshotSuffix, GenerateResultBundleReviewExportRequest request, string expectedFileName)
    {
        var response = await AssertStatus(
            () => TestClient.PostAsJsonAsync(ExportEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().EndWith(PdfExtension);
        contentDisposition.FileNameStar.Should().Be(expectedFileName);
        contentDisposition.DispositionType.Should().Be("attachment");

        // demo mock just returns the xml
        var xml = await response.Content.ReadAsStringAsync();
        var formattedXml = XmlUtil.FormatTestXml(xml);
        formattedXml.MatchRawTextSnapshot("ExportTests", "Pdf", "_snapshots", $"{request.TemplateKey}{snapshotSuffix}.xml");
    }

    protected abstract Task SeedData();

    protected abstract GenerateResultBundleReviewExportRequest NewRequest();

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ExportEndpoint, NewRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }
}
