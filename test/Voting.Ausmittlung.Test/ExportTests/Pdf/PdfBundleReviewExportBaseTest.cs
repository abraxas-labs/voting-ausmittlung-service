// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Mocks;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Pdf;

public abstract class PdfBundleReviewExportBaseTest : BaseTest<ExportService.ExportServiceClient>
{
    protected PdfBundleReviewExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected virtual ExportService.ExportServiceClient TestClient => ErfassungElectionAdminClient;

    protected abstract string NewRequestExpectedFileName { get; }

    protected abstract string TemplateKey { get; }

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

    protected async Task TestPdfReport(string snapshotSuffix, StartBundleReviewExportRequest request, string expectedFileName)
    {
        await TestClient.StartBundleReviewExportAsync(request);
        var xml = RunScoped<PdfServiceMock, string>(x => x.GetGenerated(TemplateKey));
        var formattedXml = XmlUtil.FormatTestXml(xml);
        formattedXml.MatchRawTextSnapshot("ExportTests", "Pdf", "_snapshots", $"{TemplateKey}{snapshotSuffix}.xml");

        var startedEvent = EventPublisherMock.GetSinglePublishedEvent<ProtocolExportStarted>();
        startedEvent.FileName.Should().Be(expectedFileName);
        startedEvent.ExportKey.Should().Be(TemplateKey);
    }

    protected abstract Task SeedData();

    protected abstract StartBundleReviewExportRequest NewRequest();

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .StartBundleReviewExportAsync(NewRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungRestrictedBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected async Task RunOnBundle<TEvent, TAggregate>(Guid bundleId, Action<TAggregate> bundleAction)
        where TEvent : IMessage<TEvent>
        where TAggregate : PoliticalBusinessResultBundleAggregate
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", TestDefaults.UserId, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetOrCreateById<TAggregate>(bundleId);
            bundleAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<TEvent>();
        EventPublisherMock.Clear();
    }
}
