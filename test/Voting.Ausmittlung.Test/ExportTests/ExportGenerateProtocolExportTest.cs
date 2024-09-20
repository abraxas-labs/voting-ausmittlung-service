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
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportGenerateProtocolExportTest : ExportBaseRestTest
{
    public ExportGenerateProtocolExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string BaseUrl => "/api/export/protocol/generate";

    [Fact]
    public async Task ShouldWorkForMonitoring()
    {
        await ModifyDbEntities<SimplePoliticalBusiness>(
            _ => true,
            pb => pb.EndResultFinalized = true);
        var response = await AssertStatus(() => StGallenReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()), HttpStatusCode.OK);
        var responseBody = await ReadJson<GenerateProtocolExportResponse>(response);
        responseBody.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkForErfassung()
    {
        await RemoveExistingProtocolExports();

        var response = await AssertStatus(
            async () => await StGallenReportExporterApiClient.PostAsJsonAsync(
            BaseUrl,
            NewValidRequest(x =>
            {
                x.ExportTemplateId = ExportTemplateErfassungId;
                x.CountingCircleId = CountingCircleMockedData.GuidStGallen;
            })),
            HttpStatusCode.OK);
        var responseBody = await ReadJson<GenerateProtocolExportResponse>(response);
        responseBody.MatchSnapshot();
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
        await ModifyDbEntities<SimplePoliticalBusiness>(
            _ => true,
            pb => pb.EndResultFinalized = true);
        var templateKey = AusmittlungPdfVoteTemplates.EVotingDetailsResultProtocol.Key;
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

    private GenerateProtocolExportRequest NewValidRequest(Action<GenerateProtocolExportRequest>? action = null)
    {
        var req = new GenerateProtocolExportRequest
        {
            ContestId = ContestMockedData.GuidStGallenEvoting,
            ExportTemplateId = ExportTemplateMonitoringId,
        };

        action?.Invoke(req);
        return req;
    }
}
