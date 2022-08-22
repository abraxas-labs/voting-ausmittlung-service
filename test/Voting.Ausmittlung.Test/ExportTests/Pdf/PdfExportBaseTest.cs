// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfExportBaseTest : BaseRestTest
{
    private const string ResultExportEndpoint = "/api/result_export";
    private const string PdfExtension = ".pdf";

    protected PdfExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public virtual HttpClient TestClient => MonitoringElectionAdminClient;

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
    public async Task TestPdf()
    {
        var request = NewRequest();
        var response = await AssertStatus(
            () => TestClient.PostAsJsonAsync(ResultExportEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().EndWith(PdfExtension);
        contentDisposition.FileNameStar.Should().Be(NewRequestExpectedFileName);
        contentDisposition.DispositionType.Should().Be("attachment");

        // demo mock just returns the xml
        var xml = await response.Content.ReadAsStringAsync();
        var formattedXml = XmlUtil.FormatTestXml(xml);
        formattedXml.MatchRawSnapshot("ExportTests", "Pdf", "_snapshots", SnapshotName(request) + ".xml");
    }

    protected virtual string SnapshotName(GenerateResultExportsRequest request)
        => request.ResultExportRequests[0].Key;

    protected abstract Task SeedData();

    protected abstract GenerateResultExportsRequest NewRequest();

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultExportEndpoint, NewRequest());
    }
}
