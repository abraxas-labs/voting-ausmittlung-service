// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Mocks;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfExportBaseTest : BaseTest<ExportService.ExportServiceClient>
{
    protected PdfExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected virtual ExportService.ExportServiceClient TestClient => StGallenMonitoringElectionAdminClient;

    protected abstract string NewRequestExpectedFileName { get; }

    protected abstract string TemplateKey { get; }

    protected virtual string SnapshotName => TemplateKey;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await SeedData();
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());

        // Cannot export until the end result has been finalized
        await ModifyDbEntities<SimplePoliticalBusiness>(
            _ => true,
            x => x.EndResultFinalized = true);
    }

    [Fact]
    public virtual async Task TestPdf()
    {
        await TestPdfReport(string.Empty);
    }

    protected abstract Task SeedData();

    protected abstract StartProtocolExportsRequest NewRequest();

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .StartProtocolExportsAsync(NewRequest());
    }

    protected async Task TestPdfReport(string snapshotSuffix)
    {
        var request = NewRequest();
        await TestPdfReport(snapshotSuffix, TestClient, request);
    }

    protected async Task TestPdfReport(string snapshotSuffix, ExportService.ExportServiceClient client, StartProtocolExportsRequest request)
    {
        await client.StartProtocolExportsAsync(request);
        var xml = RunScoped<PdfServiceMock, string>(x => x.GetGenerated(TemplateKey));
        var formattedXml = XmlUtil.FormatTestXml(xml);
        formattedXml.MatchRawTextSnapshot("ExportTests", "Pdf", "_snapshots", $"{SnapshotName}{snapshotSuffix}.xml");

        var startedEvent = EventPublisherMock.GetSinglePublishedEvent<ProtocolExportStarted>();
        startedEvent.FileName.Should().Be(NewRequestExpectedFileName);
    }
}
