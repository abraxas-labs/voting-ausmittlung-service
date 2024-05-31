// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Csv;

public abstract class CsvExportBaseTest : BaseRestTest
{
    private const string CsvExtension = ".csv";
    private const string CsvMimeType = "text/csv";
    private const string ResultExportEndpoint = "/api/result_export";

    protected CsvExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public virtual HttpClient TestClient => BundMonitoringElectionAdminClient;

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
    public virtual Task TestCsv() => TestCsvSnapshot(NewRequest(), NewRequestExpectedFileName);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        // Do not test the role access for each report. This is tested once for the endpoint in another test.
        yield break;
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        // Do not test the role access for each report. This is tested once for the endpoint in another test.
        yield break;
    }

    protected async Task TestCsvSnapshot(GenerateResultExportsRequest request, string expectedFileName, string? name = null)
    {
        var response = await AssertStatus(
            () => TestClient.PostAsJsonAsync(ResultExportEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(CsvMimeType);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().EndWith(CsvExtension);
        contentDisposition.FileNameStar.Should().Be(expectedFileName);
        contentDisposition.DispositionType.Should().Be("attachment");

        var csv = await response.Content.ReadAsStringAsync();
        if (name != null)
        {
            name = "_" + name;
        }

        csv.MatchRawTextSnapshot("ExportTests", "Csv", "_snapshots", GetType().Name + name + ".csv");
    }

    protected abstract Task SeedData();

    protected abstract GenerateResultExportsRequest NewRequest();

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultExportEndpoint, NewRequest());
    }
}
