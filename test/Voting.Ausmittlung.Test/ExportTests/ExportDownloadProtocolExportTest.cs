// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportDownloadProtocolExportTest : ExportBaseRestTest
{
    public ExportDownloadProtocolExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string BaseUrl => "/api/export/protocol/download";

    [Fact]
    public async Task ShouldWorkForMonitoring()
    {
        await ModifyDbEntities<SimplePoliticalBusiness>(
            _ => true,
            pb => pb.EndResultFinalized = true);
        await TestPdfDownload(() => StGallenReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()), "monitoring-mock-file.pdf");
    }

    [Fact]
    public async Task ShouldWorkForErfassung()
    {
        await TestPdfDownload(
            () => StGallenReportExporterApiClient.PostAsJsonAsync(
                BaseUrl,
                NewValidRequest(x =>
                {
                    x.ProtocolExportId = ProtocolExportErfassungId;
                    x.CountingCircleId = CountingCircleMockedData.GuidStGallen;
                })),
            "erfassung-mock-file.pdf");
    }

    [Fact]
    public async Task ShouldThrowForNonAccessibleProtocolExport()
    {
        await RemoveExistingProtocolExports();

        await AssertProblemDetails(
            () => GossauReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()),
            HttpStatusCode.BadRequest,
            "Couldn't find all protocol exports");
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(BaseUrl, NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ReportExporterApi;
    }

    private DownloadProtocolExportRequest NewValidRequest(Action<DownloadProtocolExportRequest>? action = null)
    {
        var req = new DownloadProtocolExportRequest
        {
            ContestId = ContestMockedData.GuidStGallenEvoting,
            ProtocolExportId = ProtocolExportMonitoringId,
        };

        action?.Invoke(req);
        return req;
    }
}
