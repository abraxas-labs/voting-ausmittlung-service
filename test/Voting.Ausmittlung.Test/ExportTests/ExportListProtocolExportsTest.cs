// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportListProtocolExportsTest : ExportBaseRestTest
{
    public ExportListProtocolExportsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string BaseUrl => "/api/export/protocol/list";

    [Fact]
    public async Task ShouldWorkForMonitoring()
    {
        var response = await AssertStatus(
            () => StGallenReportExporterApiClient.GetAsync(BuildUrl(ContestMockedData.GuidStGallenEvoting)),
            HttpStatusCode.OK);
        var responseBody = await ReadJson<ListProtocolExportsResponse>(response);
        responseBody.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkForErfassung()
    {
        var response = await AssertStatus(
            () => StGallenReportExporterApiClient.GetAsync(BuildUrl(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidStGallen)),
            HttpStatusCode.OK);
        var responseBody = await ReadJson<ListProtocolExportsResponse>(response);
        responseBody.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkWithExistingProtocolExports()
    {
        var responseBody = await ReadJson<ListProtocolExportsResponse>(await StGallenReportExporterApiClient.GetAsync(BuildUrl(ContestMockedData.GuidStGallenEvoting)));
        responseBody.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowForNonAccessibleCountingCircle()
    {
        await AssertProblemDetails(
            () => StGallenReportExporterApiClient.GetAsync(BuildUrl(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidUzwilKirche)),
            HttpStatusCode.Forbidden,
            "no permission entries available to access");
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.GetAsync(BuildUrl(ContestMockedData.GuidStGallenEvoting));
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ReportExporterApi;
    }
}
