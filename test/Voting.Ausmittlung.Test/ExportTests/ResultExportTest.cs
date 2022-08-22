// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using FluentAssertions;
using Voting.Ausmittlung.Controllers.Models;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ResultExportTest : BaseRestTest
{
    private const string ResultExportEndpoint = "/api/result_export";

    public ResultExportTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ExportMultipleShouldReturnZip()
    {
        var request = new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
                        },
                    },
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical.Key,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestBund),
                        },
                    },
                },
        };

        var response = await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(ResultExportEndpoint, request),
            HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(MediaTypeNames.Application.Zip);

        var contentDisposition = response.Content.Headers.ContentDisposition;
        contentDisposition!.FileNameStar.Should().Be("export.zip");
        contentDisposition.DispositionType.Should().Be("attachment");

        var zipStream = await response.Content.ReadAsStreamAsync();
        using var unzip = new ZipArchive(zipStream, ZipArchiveMode.Read, false, Encoding.UTF8);
        unzip.Entries.Count.Should().Be(2);

        foreach (var entry in unzip.Entries)
        {
            var content = await ReadZipContent(entry);
            content.MatchSnapshot(entry.Name);
        }
    }

    [Fact]
    public async Task ShouldThrowUnknownKey()
    {
        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                ResultExportEndpoint,
                new GenerateResultExportsRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    ResultExportRequests =
                    {
                        new GenerateResultExportRequest
                        {
                            Key = "unknown",
                            PoliticalBusinessIds =
                            {
                                Guid.Parse(VoteMockedData.IdStGallenVoteInContestBund),
                            },
                        },
                    },
                }),
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldThrowAnyDisabled()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisabledExportTemplateKeys.Add(AusmittlungPdfVoteTemplates.EndResultProtocol.Key);

            var request = new GenerateResultExportsRequest
            {
                ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
                        },
                    },
                },
            };

            await AssertStatus(
                () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                    ResultExportEndpoint,
                    request),
                HttpStatusCode.BadRequest);
        }
        finally
        {
            config.DisabledExportTemplateKeys.Clear();
        }
    }

    [Fact]
    public async Task ShouldThrowAllDisabled()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisableAllExports = true;

            var request = new GenerateResultExportsRequest
            {
                ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
                        },
                    },
                },
            };

            await AssertStatus(
                () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                    ResultExportEndpoint,
                    request),
                HttpStatusCode.BadRequest);
        }
        finally
        {
            config.DisableAllExports = false;
        }
    }

    [Fact]
    public async Task ShouldThrowContestWithoutPermission()
    {
        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                ResultExportEndpoint,
                new GenerateResultExportsRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdKirche),
                    ResultExportRequests =
                    {
                            new GenerateResultExportRequest
                            {
                                Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                                PoliticalBusinessIds =
                                {
                                    Guid.Parse(VoteMockedData.IdKircheVoteInContestKircheWithoutChilds),
                                },
                            },
                    },
                }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldThrowPoliticalBusinessWithoutPermission()
    {
        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                ResultExportEndpoint,
                new GenerateResultExportsRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    ResultExportRequests =
                    {
                        new GenerateResultExportRequest
                        {
                            Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                            PoliticalBusinessIds =
                            {
                                Guid.Parse(VoteMockedData.IdStGallenVoteInContestBund),
                            },
                        },
                    },
                }),
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldThrowNoDomainOfInfluence()
    {
        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                ResultExportEndpoint,
                new GenerateResultExportsRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    ResultExportRequests =
                    {
                        new GenerateResultExportRequest
                        {
                            Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                        },
                    },
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldThrowNoUnion()
    {
        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                ResultExportEndpoint,
                new GenerateResultExportsRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    ResultExportRequests =
                    {
                        new GenerateResultExportRequest
                        {
                            Key = AusmittlungPdfProportionalElectionTemplates.VoterTurnoutProtocol.Key,
                        },
                    },
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldThrowMultiplePoliticalBusinesses()
    {
        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(
                ResultExportEndpoint,
                new GenerateResultExportsRequest
                {
                    ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
                    ResultExportRequests =
                    {
                        new GenerateResultExportRequest
                        {
                            Key = AusmittlungXmlVoteTemplates.Ech0110.Key,
                            PoliticalBusinessIds = new()
                            {
                                Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
                                Guid.Parse(VoteMockedData.IdBundVote2InContestBund),
                            },
                        },
                    },
                }),
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExportShouldCreateEvents()
    {
        var request = new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                        DomainOfInfluenceType = DomainOfInfluenceType.Ch,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(VoteMockedData.IdBundVoteInContestBund),
                        },
                    },
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungCsvProportionalElectionTemplates.CandidatesNumerical.Key,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(ProportionalElectionMockedData.IdBundProportionalElectionInContestBund),
                        },
                    },
                },
        };

        await AssertStatus(
            () => BundMonitoringElectionAdminClient.PostAsJsonAsync(ResultExportEndpoint, request),
            HttpStatusCode.OK);

        var events = EventPublisherMock.GetPublishedEvents<ExportGenerated>().ToList();
        var requestId = events[0].RequestId;

        foreach (var eventData in events)
        {
            eventData.RequestId.Should().Be(requestId);
            eventData.RequestId = string.Empty;
        }

        events.Should().MatchSnapshot();
    }

    protected override Task<HttpResponseMessage> AuthorizationTestCall(HttpClient httpClient)
    {
        return httpClient.PostAsJsonAsync(ResultExportEndpoint, new GenerateResultExportsRequest
        {
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ResultExportRequests =
                {
                    new GenerateResultExportRequest
                    {
                        Key = AusmittlungPdfVoteTemplates.EndResultProtocol.Key,
                        PoliticalBusinessIds =
                        {
                            Guid.Parse(VoteMockedData.IdStGallenVoteInContestBund),
                        },
                    },
                },
        });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    private async Task<string> ReadZipContent(ZipArchiveEntry entry)
    {
        await using var entryContent = entry.Open();
        using var reader = new StreamReader(entryContent);
        return await reader.ReadToEndAsync();
    }
}
