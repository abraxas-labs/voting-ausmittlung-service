// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfExportBaseTest<T> : BaseRestTest
{
    protected const string ResultExportEndpoint = "/api/result_export";
    private const string PdfExtension = ".pdf";

    protected PdfExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public virtual HttpClient TestClient => MonitoringElectionAdminClient;

    public virtual string ExportEndpoint => ResultExportEndpoint;

    protected abstract string NewRequestExpectedFileName { get; }

    protected abstract string ContestId { get; }

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
        await TestPdfReport(string.Empty);
    }

    [Fact]
    public virtual async Task TestPdfAfterTestingPhaseEnded()
    {
        // When the testing phase ends, most of the data that was created during the testing phase gets deleted.
        // We need to test this case, as it lead to bugs (ex. VOTING-2403).
        // Most of the time, data was missing that exists most of the time, but not immediately after the testing phase has ended
        // and when no results etc. were entered.
        await TestEventPublisher.Publish(new ContestTestingPhaseEnded { ContestId = ContestId });
        await RunEvents<ContestTestingPhaseEnded>();

        await TestPdfReport("_tp_ended");
    }

    protected virtual string SnapshotName(T request)
    {
        return request switch
        {
            GenerateResultExportsRequest exportsRequest => exportsRequest.ResultExportRequests[0].Key,
            GenerateResultBundleReviewExportRequest bundleReviewExportRequest => bundleReviewExportRequest.TemplateKey,
            _ => throw new InvalidOperationException("cannot create snapshot name from request"),
        };
    }

    protected abstract Task SeedData();

    protected abstract T NewRequest();

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ExportEndpoint, NewRequest());
    }

    private async Task TestPdfReport(string snapshotSuffix)
    {
        var request = NewRequest();
        var response = await AssertStatus(
            () => TestClient.PostAsJsonAsync(ExportEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Pdf);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().EndWith(PdfExtension);
        contentDisposition.FileNameStar.Should().Be(NewRequestExpectedFileName);
        contentDisposition.DispositionType.Should().Be("attachment");

        // demo mock just returns the xml
        var xml = await response.Content.ReadAsStringAsync();
        var formattedXml = XmlUtil.FormatTestXml(xml);
        formattedXml.MatchRawSnapshot("ExportTests", "Pdf", "_snapshots", $"{SnapshotName(request)}{snapshotSuffix}.xml");
    }
}
