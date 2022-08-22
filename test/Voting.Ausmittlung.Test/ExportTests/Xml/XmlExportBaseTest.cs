// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests.Xml;

public abstract class XmlExportBaseTest<T> : BaseRestTest
{
    protected const string ResultExportEndpoint = "/api/result_export";
    private const string XmlExtension = ".xml";

    protected XmlExportBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public virtual HttpClient TestClient => MonitoringElectionAdminClient;

    protected abstract string NewRequestExpectedFileName { get; }

    public override async Task InitializeAsync()
    {
        var contestId = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var countingCircleId = CountingCircleMockedData.GuidGossau;

        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);

        await RunOnDb(async db =>
        {
            var details = await db.ContestCountingCircleDetails
                .AsTracking()
                .SingleAsync(x => x.ContestId == contestId && x.CountingCircle.BasisCountingCircleId == countingCircleId);
            details.TotalCountOfVoters = 5000;
            await db.SaveChangesAsync();
        });

        await SeedData();
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestXml()
    {
        var request = NewRequest();
        var response = await AssertStatus(
            () => TestClient.PostAsJsonAsync(ResultExportEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Xml);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().EndWith(XmlExtension);
        contentDisposition.FileNameStar.Should().Be(NewRequestExpectedFileName);
        contentDisposition.DispositionType.Should().Be("attachment");

        var xml = await response.Content.ReadAsStringAsync() ?? string.Empty;

        // Order of XML fields isn't deterministic. Deserialize to get a deterministic snapshot
        var deserialized = EchDeserializer.FromXml<T>(xml);
        CleanDataForSnapshot(deserialized);
        deserialized.ShouldMatchChildSnapshot(GetType().Name);
    }

    protected abstract Task SeedData();

    protected abstract GenerateResultExportsRequest NewRequest();

    protected abstract void CleanDataForSnapshot(T data);

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultExportEndpoint, NewRequest());
    }
}
