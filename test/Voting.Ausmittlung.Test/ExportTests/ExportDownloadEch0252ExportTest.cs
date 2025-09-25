// (c) Copyright by Abraxas Informatik AG
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
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.TemporaryData;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Ech.Ech0252_2_0.Schemas;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Mocks;
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
        await ModifyDbEntities<VoteResult>(x => true, x =>
        {
            x.SubmissionDoneTimestamp = new DateTime(2022, 10, 22, 12, 3, 0, DateTimeKind.Utc);
            x.State = CountingCircleResultState.SubmissionDone;
        });

        await ModifyDbEntities<ProportionalElectionResult>(x => true, x =>
        {
            x.AuditedTentativelyTimestamp = new DateTime(2022, 4, 11, 12, 3, 0, DateTimeKind.Utc);
            x.State = CountingCircleResultState.AuditedTentatively;
        });

        await TestExport(NewValidRequest(), StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(9);

            var formattedXmlVotes = ValidateAndFormat(archive, "eCH-0252_vote-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlVotes.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252VotesApi.xml");

            var formattedXmlProportionalElections = ValidateAndFormat(archive, "eCH-0252_proportional-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApi.xml");

            var formattedXmlMajorityElections = ValidateAndFormat(archive, "eCH-0252_majority-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsApi.xml");
        });
    }

    [Fact]
    public async Task ShouldWorkWithCandidateListResultsInfo()
    {
        await ModifyDbEntities<ProportionalElectionResult>(x => true, x =>
        {
            x.AuditedTentativelyTimestamp = new DateTime(2022, 4, 11, 12, 3, 0, DateTimeKind.Utc);
            x.State = CountingCircleResultState.AuditedTentatively;
        });

        await RunOnDb(async db =>
        {
            var election = await db.ProportionalElections
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.EndResult)
                .Include(x => x.Results.OrderBy(r => r.CountingCircleId))
                .ThenInclude(x => x.ListResults)
                .ThenInclude(x => x.CandidateResults)
                .ThenInclude(x => x.VoteSources)
                .FirstAsync(x => x.Id == ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id);

            election.EndResult!.MandateDistributionTriggered = true;

            // Only do this on one result. That should be enough to show it in the XML
            var listResults = election.Results.First().ListResults;
            var listIds = listResults
                .OrderBy(x => x.ListId)
                .Select(x => x.ListId)
                .ToList();
            var candidates = listResults.SelectMany(x => x.CandidateResults);
            foreach (var candidate in candidates)
            {
                candidate.VoteSources.Add(new ProportionalElectionCandidateVoteSourceResult
                {
                    ConventionalVoteCount = 3,
                });

                foreach (var (listId, index) in listIds.Select((x, i) => (x, i)))
                {
                    candidate.VoteSources.Add(new ProportionalElectionCandidateVoteSourceResult
                    {
                        ConventionalVoteCount = index + 1,
                        EVotingVoteCount = index,
                        ListId = listId,
                    });
                }
            }

            await db.SaveChangesAsync();
        });

        var request = NewValidRequest(x =>
        {
            x.IncludeCandidateListResultsInfo = true;
            x.PoliticalBusinessTypes = [PoliticalBusinessType.ProportionalElection];
        });
        await TestExport(request, StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(3);

            var formattedXmlProportionalElections = ValidateAndFormat(archive, "eCH-0252_proportional-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApiWithCandidateResultsInfo.xml");

            // This XML should NOT contain candidate list results infos, as the proportional election is not finished yet
            var formattedXmlProportionalElectionsWithoutInfos = ValidateAndFormat(archive, "eCH-0252_proportional-election-result-delivery_20200302_cc70fe43-8f4e-4bc6-a461-b808907bc996.xml");
            formattedXmlProportionalElectionsWithoutInfos.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApiWithoutCandidateResultsInfo.xml");
        });
    }

    [Fact]
    public async Task ShouldWorkWithoutPublished()
    {
        var voteResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteMockedData.StGallenVoteInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<VoteResult>(x => x.Id == voteResultId, x => x.Published = false);

        var proportionalElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<ProportionalElectionResult>(x => x.Id == proportionalElectionResultId, x => x.Published = false);

        var majorityElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<MajorityElectionResult>(x => x.Id == majorityElectionResultId, x => x.Published = false);

        await TestExport(NewValidRequest(), StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(9);

            var formattedXmlVotes = ValidateAndFormat(archive, "eCH-0252_vote-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlVotes.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252VotesApiWithoutPublished.xml");

            var formattedXmlProportionalElections = ValidateAndFormat(archive, "eCH-0252_proportional-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApiWithoutPublished.xml");

            var formattedXmlMajorityElections = ValidateAndFormat(archive, "eCH-0252_majority-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsApiWithoutPublished.xml");
        });
    }

    [Fact]
    public async Task ShouldWorkFilteredStatesIncludedButNoResultData()
    {
        var voteResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteMockedData.StGallenVoteInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<VoteResult>(x => x.Id == voteResultId, x => x.State = CountingCircleResultState.SubmissionOngoing);

        var proportionalElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<ProportionalElectionResult>(x => x.Id == proportionalElectionResultId, x => x.State = CountingCircleResultState.SubmissionOngoing);

        var majorityElectionResultId = AusmittlungUuidV5.BuildPoliticalBusinessResult(MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id, CountingCircleMockedData.GuidStGallen, false);
        await ModifyDbEntities<MajorityElectionResult>(x => x.Id == majorityElectionResultId, x => x.State = CountingCircleResultState.SubmissionOngoing);

        await TestExport(NewValidRequest(x => x.CountingStates = [CountingCircleResultState.SubmissionDone]), StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(9);

            var formattedXmlVotes = ValidateAndFormat(archive, "eCH-0252_vote-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlVotes.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252VotesApiFilteredStates.xml");

            var formattedXmlProportionalElections = ValidateAndFormat(archive, "eCH-0252_proportional-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApiFilteredStates.xml");

            var formattedXmlMajorityElections = ValidateAndFormat(archive, "eCH-0252_majority-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsApiFilteredStates.xml");
        });
    }

    [Fact]
    public async Task ValidContestButInvalidVotingIdentificationsShouldReturnEmptyFiles()
    {
        var request = new DownloadEch0252ExportRequest
        {
            PollingDate = new DateOnly(2020, 8, 31),
            VotingIdentifications = [Guid.Empty],
        };

        await TestExport(request, StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(3);
            var formattedXmlVotes = ValidateAndFormat(archive, "eCH-0252_vote-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlVotes.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252VotesApiEmptyFile.xml");

            var formattedXmlProportionalElections = ValidateAndFormat(archive, "eCH-0252_proportional-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsApiEmptyFile.xml");

            var formattedXmlMajorityElections = ValidateAndFormat(archive, "eCH-0252_majority-election-result-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsApiEmptyFile.xml");
        });
    }

    [Fact]
    public async Task NonCantonSettingsAdminShouldReturnEmpty()
    {
        var request = new DownloadEch0252ExportRequest
        {
            PollingDate = new DateOnly(2020, 3, 2),
        };

        await TestExport(request, UzwilReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(0);
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
            archive.Entries.Any(e => e.FullName == "eCH-0252_vote-result-delivery_20200302_cc70fe43-8f4e-4bc6-a461-b808907bc996.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252_proportional-election-result-delivery_20200302_cc70fe43-8f4e-4bc6-a461-b808907bc996.xml").Should().BeTrue();
            archive.Entries.Any(e => e.FullName == "eCH-0252_majority-election-result-delivery_20200302_cc70fe43-8f4e-4bc6-a461-b808907bc996.xml").Should().BeTrue();
        });
    }

    [Fact]
    public async Task ShouldWorkWithInformationOnly()
    {
        await RunOnDb(async db =>
        {
            db.ProportionalElectionUnions.Add(new ProportionalElectionUnion()
            {
                Id = Guid.Parse("8cdc91a4-5ec5-46e1-8872-cad60b907300"),
                ProportionalElectionUnionEntries = new List<ProportionalElectionUnionEntry>
                {
                    new() { ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen) },
                },
                ContestId = ContestMockedData.GuidStGallenEvoting,
                Description = "Kantonratswahl 2020",
            });

            await db.SaveChangesAsync();
        });

        await ModifyDbEntities<ProportionalElectionCandidate>(
            x => x.Id == Guid.Parse(ProportionalElectionMockedData.CandidateIdStGallenProportionalElectionInContestStGallen),
            x => x.PartyId = Guid.Parse(DomainOfInfluenceMockedData.PartyIdBundAndere));

        // Patch long descriptions since otherwise the eCH would be invalid
        await ModifyDbEntities<ProportionalElectionListTranslation>(
            x => x.Description == string.Empty,
            x => x.Description = "long description");

        var request = new DownloadEch0252ExportRequest
        {
            PollingDate = new DateOnly(2020, 8, 31),
            InformationOnly = true,
        };

        await TestExport(request, StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(3);

            using var entryStreamProportionalElections = archive.Entries.Single(e => e.FullName == "eCH-0252_proportional-election-info-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml").Open();
            using var srProportionalElections = new StreamReader(entryStreamProportionalElections);
            var xmlProportionalElections = srProportionalElections.ReadToEnd();
            var formattedXmlProportionalElections = XmlUtil.FormatTestXml(xmlProportionalElections);
            formattedXmlProportionalElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252ProportionalElectionsInformationApi.xml");

            using var entryStreamMajorityElections = archive.Entries.Single(e => e.FullName == "eCH-0252_majority-election-info-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml").Open();
            using var srMajorityElections = new StreamReader(entryStreamMajorityElections);
            var xmlMajorityElections = srMajorityElections.ReadToEnd();
            var formattedXmlMajorityElections = XmlUtil.FormatTestXml(xmlMajorityElections);
            formattedXmlMajorityElections.MatchRawTextSnapshot("ExportTests", "Xml", "_snapshots", "XmlEch0252MajorityElectionsInformationApi.xml");
        });
    }

    [Fact]
    public async Task ShouldWorkWithPoliticalBusinessFilter()
    {
        var request = new DownloadEch0252ExportRequest
        {
            PollingDate = new DateOnly(2020, 8, 31),
            PoliticalBusinessTypes = [PoliticalBusinessType.MajorityElection],
            InformationOnly = true,
        };

        await TestExport(request, StGallenReportExporterApiClient, archive =>
        {
            archive.Entries.Count.Should().Be(1);
            var entry = archive.Entries.Single();
            entry.FullName.Should().Be("eCH-0252_majority-election-info-delivery_20200831_95825eb0-0f52-461a-a5f8-23fb35fa69e1.xml");
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

    [Fact]
    public async Task ShouldThrowIfApiLimitReached()
    {
        var tenantId = SecureConnectTestDefaults.MockedTenantStGallen.Id;

        await RunScoped<TemporaryDataContext>(async dbContext =>
        {
            dbContext.ExportLogEntries.AddRange(
                new()
                {
                    TenantId = tenantId,
                    ExportKey = "eCH-0252-api",
                    Timestamp = MockedClock.UtcNowDate,
                },
                new()
                {
                    TenantId = tenantId,
                    ExportKey = "eCH-0252-api",
                    Timestamp = MockedClock.UtcNowDate,
                });
            await dbContext.SaveChangesAsync();
        });

        await AssertProblemDetails(
            () => StGallenReportExporterApiClient.PostAsJsonAsync(BaseUrl, NewValidRequest()),
            HttpStatusCode.TooManyRequests,
            "Rate limit reached");
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

    private string ValidateAndFormat(ZipArchive archive, string entryName)
    {
        using var entryStream = archive.Entries.Single(e => e.FullName == entryName).Open();
        using var sr = new StreamReader(entryStream);
        var xml = sr.ReadToEnd();
        XmlUtil.ValidateSchema(xml, Ech0252Schemas.LoadEch0252Schemas());
        return XmlUtil.FormatTestXml(xml);
    }
}
