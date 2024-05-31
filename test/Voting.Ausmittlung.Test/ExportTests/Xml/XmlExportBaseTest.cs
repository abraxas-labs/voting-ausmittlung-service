﻿// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Schema;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
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
        await TestXmlWithSnapshot();
    }

    protected async Task TestXmlWithSnapshot(string snapshotSuffix = "")
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

        var xml = await response.Content.ReadAsStringAsync();
        XmlUtil.ValidateSchema(xml, GetSchemaSet());
        MatchXmlSnapshot(xml, $"{GetType().Name}{snapshotSuffix}");
    }

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

    protected abstract Task SeedData();

    protected abstract GenerateResultExportsRequest NewRequest();

    protected abstract XmlSchemaSet GetSchemaSet();

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultExportEndpoint, NewRequest());
    }

    private void MatchXmlSnapshot(string xml, string fileName)
    {
        xml = XmlUtil.FormatTestXml(xml);
        xml.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", fileName + ".xml");
    }
}
