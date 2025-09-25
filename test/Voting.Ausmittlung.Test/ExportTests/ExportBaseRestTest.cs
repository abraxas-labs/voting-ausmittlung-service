// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentAssertions;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.VotingExports.Repository.Ausmittlung;

namespace Voting.Ausmittlung.Test.ExportTests;

public abstract class ExportBaseRestTest : BaseRestTest
{
    private const string CsvExtension = ".csv";
    private const string CsvMimeType = "text/csv";
    private const string PdfExtension = ".pdf";
    private const string ZipExtension = ".zip";

    private Lazy<HttpClient> _stGallenReportExporterApiClient;
    private Lazy<HttpClient> _gossauReportExporterApiClient;
    private Lazy<HttpClient> _uzwilExporterApiClient;

    protected ExportBaseRestTest(TestApplicationFactory factory)
        : base(factory)
    {
        _stGallenReportExporterApiClient = new Lazy<HttpClient>(() => CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantStGallen.Id,
            roles: RolesMockedData.ReportExporterApi));

        _gossauReportExporterApiClient = new Lazy<HttpClient>(() => CreateHttpClient(
            tenant: SecureConnectTestDefaults.MockedTenantGossau.Id,
            roles: RolesMockedData.ReportExporterApi));

        _uzwilExporterApiClient = new Lazy<HttpClient>(() => CreateHttpClient(
             tenant: SecureConnectTestDefaults.MockedTenantUzwil.Id,
             roles: RolesMockedData.ReportExporterApi));
    }

    protected abstract string BaseUrl { get; }

    protected Guid ExportTemplateMonitoringId => AusmittlungUuidV5.BuildExportTemplate(
        AusmittlungPdfVoteTemplates.EVotingDetailsResultProtocol.Key,
        SecureConnectTestDefaults.MockedTenantStGallen.Id);

    protected Guid ExportTemplateErfassungId => AusmittlungUuidV5.BuildExportTemplate(
        AusmittlungPdfProportionalElectionTemplates.ListsCountingCircleProtocol.Key,
        SecureConnectTestDefaults.MockedTenantStGallen.Id,
        countingCircleId: CountingCircleMockedData.GuidStGallen,
        politicalBusinessId: Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen));

    protected Guid ProtocolExportMonitoringId => AusmittlungUuidV5.BuildProtocolExport(
        ContestMockedData.GuidStGallenEvoting,
        false,
        ExportTemplateMonitoringId);

    protected Guid ProtocolExportErfassungId => AusmittlungUuidV5.BuildProtocolExport(
        ContestMockedData.GuidStGallenEvoting,
        false,
        ExportTemplateErfassungId);

    protected HttpClient StGallenReportExporterApiClient => _stGallenReportExporterApiClient.Value;

    protected HttpClient GossauReportExporterApiClient => _gossauReportExporterApiClient.Value;

    protected HttpClient UzwilReportExporterApiClient => _uzwilExporterApiClient.Value;

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
            db.ProtocolExports.Add(new()
            {
                Id = ProtocolExportMonitoringId,
                FileName = "monitoring-mock-file.pdf",
                Started = new DateTime(2020, 11, 5, 5, 0, 0, DateTimeKind.Utc),
                State = Data.Models.ProtocolExportState.Completed,
                ContestId = ContestMockedData.GuidStGallenEvoting,
                ExportTemplateId = ExportTemplateMonitoringId,
                PoliticalBusinessIds = [VoteMockedData.StGallenVoteInContestStGallen.Id],
            });
            db.ProtocolExports.Add(new()
            {
                Id = ProtocolExportErfassungId,
                FileName = "erfassung-mock-file.pdf",
                Started = new DateTime(2022, 2, 10, 9, 15, 0, DateTimeKind.Utc),
                State = Data.Models.ProtocolExportState.Completed,
                ContestId = ContestMockedData.GuidStGallenEvoting,
                ExportTemplateId = ExportTemplateErfassungId,
                PoliticalBusinessIds = [ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id],
            });
            await db.SaveChangesAsync();
        });
    }

    protected Task TestCsvDownload(Func<Task<HttpResponseMessage>> apiCall, string expectedFileName) =>
        TestDownload(apiCall, CsvMimeType, CsvExtension, expectedFileName);

    protected Task TestPdfDownload(Func<Task<HttpResponseMessage>> apiCall, string expectedFileName) =>
        TestDownload(apiCall, MediaTypeNames.Application.Pdf, PdfExtension, expectedFileName);

    protected Task TestZipDownload(Func<Task<HttpResponseMessage>> apiCall, string expectedFileName) =>
        TestDownload(apiCall, MediaTypeNames.Application.Zip, ZipExtension, expectedFileName);

    protected async Task RemoveExistingProtocolExports()
    {
        await RunOnDb(async db =>
        {
            db.ProtocolExports.RemoveRange(db.ProtocolExports.ToArray());
            await db.SaveChangesAsync();
        });
    }

    protected override HttpClient CreateHttpClient(params string[] roles)
        => CreateHttpClient(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected string BuildUrl(Guid contestId, Guid? countingCircleId = null)
        => $"{BaseUrl}?contestId={contestId}{(countingCircleId.HasValue ? $"&countingCircleId={countingCircleId}" : string.Empty)}";

    private async Task TestDownload(Func<Task<HttpResponseMessage>> apiCall, string expectedMediaType, string fileExtension, string expectedFileName)
    {
        var response = await AssertStatus(apiCall, HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType.Should().Be(expectedMediaType);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().EndWith(fileExtension);
        contentDisposition.FileNameStar.Should().Be(expectedFileName);
        contentDisposition.DispositionType.Should().Be("attachment");
    }
}
