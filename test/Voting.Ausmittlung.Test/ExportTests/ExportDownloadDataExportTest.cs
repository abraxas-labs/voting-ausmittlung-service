// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Utils;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportDownloadDataExportTest : ExportBaseRestTest
{
    public ExportDownloadDataExportTest(TestApplicationFactory factory)
        : base(factory)
    {
        ResetTempDb();
    }

    protected override string BaseUrl => "/api/export/data/download";

    [Fact]
    public async Task ShouldWorkForMonitoring()
    {
        await TestCsvDownload(() => StGallenReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()), "WM_Gemeinden.csv");
    }

    [Fact]
    public async Task ShouldWorkForErfassung()
    {
        await TestCsvDownload(
            () => StGallenReportExporterApiClient.PostAsJsonAsync(
                BaseUrl,
                NewValidRequest(x =>
                {
                    x.ExportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
                        AusmittlungCsvProportionalElectionTemplates.CandidatesAlphabetical.Key,
                        SecureConnectTestDefaults.MockedTenantStGallen.Id,
                        countingCircleId: CountingCircleMockedData.GuidStGallen,
                        politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen));
                    x.CountingCircleId = CountingCircleMockedData.GuidStGallen;
                })),
            "Kandidatinnen und Kandidaten.csv");
    }

    [Fact]
    public async Task ShouldThrowForNonAccessibleExportTemplate()
    {
        await AssertProblemDetails(
            () => GossauReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()),
            HttpStatusCode.BadRequest,
            "Invalid export template IDs provided");
    }

    [Fact]
    public async Task ShouldThrowIfApiLimitReached()
    {
        var templateKey = AusmittlungWabstiCTemplates.WMGemeinden.Key;
        var tenantId = SecureConnectTestDefaults.MockedTenantStGallen.Id;

        await RunScoped<TemporaryDataContext>(async dbContext =>
        {
            dbContext.ExportLogEntries.AddRange(
                new()
                {
                    TenantId = tenantId,
                    ExportKey = templateKey,
                    Timestamp = MockedClock.UtcNowDate,
                },
                new()
                {
                    TenantId = tenantId,
                    ExportKey = templateKey,
                    Timestamp = MockedClock.UtcNowDate,
                });
            await dbContext.SaveChangesAsync();
        });

        await AssertProblemDetails(
            () => StGallenReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()),
            HttpStatusCode.Forbidden,
            "Rate limit reached");
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(BaseUrl, NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ReportExporterApi;
    }

    private DownloadDataExportRequest NewValidRequest(Action<DownloadDataExportRequest>? action = null)
    {
        var req = new DownloadDataExportRequest
        {
            ContestId = ContestMockedData.GuidStGallenEvoting,
            ExportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
                AusmittlungWabstiCTemplates.WMGemeinden.Key,
                SecureConnectTestDefaults.MockedTenantStGallen.Id),
        };

        action?.Invoke(req);
        return req;
    }

    private void ResetTempDb()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var temporaryDb = scope.ServiceProvider.GetRequiredService<TemporaryDataContext>();
        DatabaseUtil.Truncate(db, temporaryDb);
    }
}
