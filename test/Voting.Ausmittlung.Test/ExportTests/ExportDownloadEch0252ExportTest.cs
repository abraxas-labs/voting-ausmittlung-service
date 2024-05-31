// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportDownloadEch0252ExportTest : ExportBaseRestTest
{
    public ExportDownloadEch0252ExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override string BaseUrl => "/api/export/ech0252/download";

    [Fact]
    public async Task ShouldWork()
    {
        await TestExport(NewValidRequest(), StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(9);
            archive.Entries.Any(e => e.FullName == "eCH-0252-votes-20200302.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252-proportional-elections-20200302.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252-majority-elections-20200302.xml").Should().BeTrue();

            using var entryStreamVotes = archive.Entries.Single(e => e.FullName == "eCH-0252-votes-20200831.xml").Open();
            using var srVotes = new StreamReader(entryStreamVotes);
            var xmlVotes = srVotes.ReadToEnd();
            var formattedXmlVotes = XmlUtil.FormatTestXml(xmlVotes);
            formattedXmlVotes.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252VotesApi.xml");

            using var entryStreamProportionalElections = archive.Entries.Single(e => e.FullName == "eCH-0252-proportional-elections-20200831.xml").Open();
            using var srProportionalElections = new StreamReader(entryStreamProportionalElections);
            var xmlProportionalElections = srProportionalElections.ReadToEnd();
            var formattedXmlProportionalElections = XmlUtil.FormatTestXml(xmlProportionalElections);
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApi.xml");

            using var entryStreamMajorityElections = archive.Entries.Single(e => e.FullName == "eCH-0252-majority-elections-20200831.xml").Open();
            using var srMajorityElections = new StreamReader(entryStreamMajorityElections);
            var xmlMajorityElections = srMajorityElections.ReadToEnd();
            var formattedXmlMajorityElections = XmlUtil.FormatTestXml(xmlMajorityElections);
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsApi.xml");
        });
    }

    [Fact]
    public async Task ShouldWorkWithoutPublished()
    {
        var voteResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteMockedData.BundVoteInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<VoteResult>(x => x.Id == voteResultId, x => x.Published = false);

        var proportionalElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(ProportionalElectionMockedData.BundProportionalElectionInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<ProportionalElectionResult>(x => x.Id == proportionalElectionResultId, x => x.Published = false);

        var majorityElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(MajorityElectionMockedData.BundMajorityElectionInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<MajorityElectionResult>(x => x.Id == majorityElectionResultId, x => x.Published = false);

        await TestExport(NewValidRequest(), StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(9);
            archive.Entries.Any(e => e.FullName == "eCH-0252-votes-20200302.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252-proportional-elections-20200302.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252-majority-elections-20200302.xml").Should().BeTrue();

            using var entryStreamVotes = archive.Entries.Single(e => e.FullName == "eCH-0252-votes-20200831.xml").Open();
            using var srVotes = new StreamReader(entryStreamVotes);
            var xmlVotes = srVotes.ReadToEnd();
            var formattedXmlVotes = XmlUtil.FormatTestXml(xmlVotes);
            formattedXmlVotes.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252VotesApiWithoutPublished.xml");

            using var entryStreamProportionalElections = archive.Entries.Single(e => e.FullName == "eCH-0252-proportional-elections-20200831.xml").Open();
            using var srProportionalElections = new StreamReader(entryStreamProportionalElections);
            var xmlProportionalElections = srProportionalElections.ReadToEnd();
            var formattedXmlProportionalElections = XmlUtil.FormatTestXml(xmlProportionalElections);
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApiWithoutPublished.xml");

            using var entryStreamMajorityElections = archive.Entries.Single(e => e.FullName == "eCH-0252-majority-elections-20200831.xml").Open();
            using var srMajorityElections = new StreamReader(entryStreamMajorityElections);
            var xmlMajorityElections = srMajorityElections.ReadToEnd();
            var formattedXmlMajorityElections = XmlUtil.FormatTestXml(xmlMajorityElections);
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsApiWithoutPublished.xml");
        });
    }

    [Fact]
    public async Task ShouldWorkWithExactDate()
    {
        var request = new DownloadEch0252ExportRequest
        {
            PollingDate = new DateOnly(2020, 3, 2),
        };

        await TestExport(request, StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(3);
            archive.Entries.Any(e => e.FullName == "eCH-0252-votes-20200302.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252-proportional-elections-20200302.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252-majority-elections-20200302.xml").Should().BeTrue();
        });
    }

    [Fact]
    public async Task ShouldReturnEmptyZipWithNoContest()
    {
        var request = new DownloadEch0252ExportRequest
        {
            PollingDate = new DateOnly(2020, 3, 2),
        };

        await TestExport(request, GossauReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(0);
        });
    }

    [Fact]
    public async Task ShouldThrowIfInvalidDateFilter()
    {
        var request = new DownloadEch0252ExportRequest
        {
            PollingDateFrom = new DateOnly(2020, 1, 1),
            PollingDateTo = new DateOnly(2021, 1, 1),
            PollingDate = new DateOnly(2020, 3, 2),
        };

        await AssertProblemDetails(
            () => StGallenReportExporterApiClient.PostAsJsonAsync(BaseUrl, request),
            HttpStatusCode.BadRequest,
            "Only one date filter is allowed and required.");
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ModifyDbEntities<VoteResult>(_ => true, x => x.Published = true);
        await ModifyDbEntities<MajorityElectionResult>(_ => true, x => x.Published = true);
        await ModifyDbEntities<ProportionalElectionResult>(_ => true, x => x.Published = true);
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(BaseUrl, NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ReportExporterApi;
    }

    private DownloadEch0252ExportRequest NewValidRequest(Action<DownloadEch0252ExportRequest>? action = null)
    {
        var req = new DownloadEch0252ExportRequest
        {
            PollingDateFrom = new DateOnly(2020, 1, 1),
            PollingDateTo = new DateOnly(2021, 1, 1),
        };

        action?.Invoke(req);
        return req;
    }

    private async Task TestExport(DownloadEch0252ExportRequest request, HttpClient client, Action<ZipArchive> action)
    {
        await TestZipDownload(
            async () =>
            {
                var response = await client.PostAsJsonAsync(BaseUrl, request);
                using var archive = new ZipArchive(response.Content.ReadAsStream());
                action.Invoke(archive);
                return response;
            },
            "export.zip");
    }
}
