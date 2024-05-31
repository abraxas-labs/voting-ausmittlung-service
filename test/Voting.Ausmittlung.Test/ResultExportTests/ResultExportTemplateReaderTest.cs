// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Controllers.Models.Export;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Services.Read;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Models;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultExportTests;

public class ResultExportTemplateReaderTest : BaseIntegrationTest
{
    public ResultExportTemplateReaderTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldWork()
    {
        var pdfTemplatesMonitoring = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null, new[] { ExportFileFormat.Pdf });
        var csvTemplatesMonitoring = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null, new[] { ExportFileFormat.Csv });
        var xmlTemplatesMonitoring = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null, new[] { ExportFileFormat.Xml });

        var pdfTemplatesErfassung = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidStGallen, new[] { ExportFileFormat.Pdf });
        var csvTemplatesErfassung = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidStGallen, new[] { ExportFileFormat.Csv });
        var xmlTemplatesErfassung = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidStGallen, new[] { ExportFileFormat.Xml });

        pdfTemplatesMonitoring.MatchSnapshot("PdfMonitoring");
        csvTemplatesMonitoring.MatchSnapshot("CsvMonitoring");
        xmlTemplatesMonitoring.MatchSnapshot("XmlMonitoring");
        pdfTemplatesErfassung.MatchSnapshot("PdfErfassung");
        csvTemplatesErfassung.MatchSnapshot("CsvErfassung");
        xmlTemplatesErfassung.MatchSnapshot("XmlErfassung");
    }

    [Fact]
    public async Task AllDisabledShouldReturnEmptyTemplates()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisableAllExports = true;
            var result = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);
            result.Should().BeEmpty();
        }
        finally
        {
            config.DisableAllExports = false;
        }
    }

    [Fact]
    public async Task DisabledExportKeyShouldIgnore()
    {
        var key = AusmittlungPdfMajorityElectionTemplates.CountingCircleProtocol.Key;
        var config = GetService<PublisherConfig>();
        try
        {
            var resultBefore = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidStGallen);
            config.DisabledExportTemplateKeys.Add(key);
            var resultAfter = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidStGallen);

            resultBefore.Should().HaveCountGreaterThan(0);
            resultAfter.Count.Should().BeLessThan(resultBefore.Count);
        }
        finally
        {
            GetService<PublisherConfig>().DisabledExportTemplateKeys.Clear();
        }
    }

    [Fact]
    public async Task EndResultFinalizationShouldFilterPdfs()
    {
        await ModifyDbEntities<SimplePoliticalBusiness>(_ => true, pb => pb.EndResultFinalized = false);
        var resultBefore = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);

        await ModifyDbEntities<SimplePoliticalBusiness>(_ => true, pb => pb.EndResultFinalized = true);
        var resultAfter = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);

        resultBefore.Should().HaveCountGreaterThan(0);
        resultAfter.Count.Should().BeGreaterThan(resultBefore.Count);
    }

    [Fact]
    public async Task ActiveContestInMonitoringShouldIncludeActivityProtocol()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.GuidStGallenEvoting,
            c => c.State = ContestState.Active);

        var result = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfContestTemplates.ActivityProtocol.Key,
            CountingCircleMockedData.StGallen.ResponsibleAuthority.SecureConnectId).ToString();

        result.Should().Contain(x => x.ExportTemplateId == exportTemplateId);
    }

    [Theory]
    [InlineData(ContestState.TestingPhase, false, false)]
    [InlineData(ContestState.TestingPhase, true, false)]
    [InlineData(ContestState.Active, false, true)]
    [InlineData(ContestState.Active, true, false)]
    public async Task TestActivityProtocol(ContestState state, bool withCountingCircle, bool expectedActivityProtocolProtocol)
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.GuidStGallenEvoting,
            c => c.State = state);

        var result = await FetchExportTemplates(
            ContestMockedData.GuidStGallenEvoting,
            withCountingCircle ? CountingCircleMockedData.GuidStGallen : null);

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfContestTemplates.ActivityProtocol.Key,
            CountingCircleMockedData.StGallen.ResponsibleAuthority.SecureConnectId).ToString();

        result.Any(x => x.ExportTemplateId == exportTemplateId).Should().Be(expectedActivityProtocolProtocol);
    }

    [Fact]
    public async Task ActiveContestInMonitoringNotContestManagerShouldFilterActivityProtocol()
    {
        await ModifyDbEntities<Contest>(
            c => c.Id == ContestMockedData.GuidStGallenEvoting,
            c => c.State = ContestState.Active);

        var result = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null, tenantId: SecureConnectTestDefaults.MockedTenantGossau.Id);

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfContestTemplates.ActivityProtocol.Key,
            CountingCircleMockedData.StGallen.ResponsibleAuthority.SecureConnectId).ToString();

        result.Should().NotContain(x => x.ExportTemplateId == exportTemplateId);
    }

    [Fact]
    public async Task InvalidVotesShouldFilter()
    {
        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            politicalBusinessId: MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id)
            .ToString();

        await ModifyDbEntities<SimplePoliticalBusiness>(
            x => x.PoliticalBusinessType == PoliticalBusinessType.MajorityElection,
            pb => pb.EndResultFinalized = true);
        await ModifyDbEntities<ContestCantonDefaults>(_ => true, x => x.MajorityElectionInvalidVotes = true, true);

        var resBefore = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);
        resBefore.Should().Contain(x => x.ExportTemplateId == exportTemplateId);

        await ModifyDbEntities<ContestCantonDefaults>(_ => true, x => x.MajorityElectionInvalidVotes = false, true);

        var resAfter = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);
        resAfter.Should().NotContain(x => x.ExportTemplateId == exportTemplateId);
    }

    [Fact]
    public async Task CountingCircleEVotingShouldFilter()
    {
        var tenantId = SecureConnectTestDefaults.MockedTenantGossau.Id;

        var exportTemplateIds = new List<string>
        {
            AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungPdfMajorityElectionTemplates.CountingCircleEVotingProtocol.Key,
                    tenantId,
                    countingCircleId: CountingCircleMockedData.GuidGossau,
                    politicalBusinessId: MajorityElectionMockedData.GossauMajorityElectionInContestStGallen.Id)
                .ToString(),
            AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungPdfProportionalElectionTemplates.ListVotesCountingCircleEVotingProtocol.Key,
                    tenantId,
                    countingCircleId: CountingCircleMockedData.GuidGossau,
                    politicalBusinessId: ProportionalElectionMockedData.GossauProportionalElectionInContestStGallen.Id)
                .ToString(),
            AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungPdfProportionalElectionTemplates.ListCandidateEmptyVotesCountingCircleEVotingProtocol.Key,
                    tenantId,
                    countingCircleId: CountingCircleMockedData.GuidGossau,
                    politicalBusinessId: ProportionalElectionMockedData.GossauProportionalElectionInContestStGallen.Id)
                .ToString(),
        };

        await ModifyDbEntities<SimplePoliticalBusiness>(
            x => x.PoliticalBusinessType == PoliticalBusinessType.MajorityElection,
            pb => pb.EndResultFinalized = true);

        var resultBefore = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidGossau, tenantId: tenantId);
        resultBefore.Where(x => exportTemplateIds.Contains(x.ExportTemplateId)).Should().NotBeEmpty();

        await ModifyDbEntities<ContestCountingCircleDetails>(
            x => x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.GuidGossau && x.ContestId == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            x => x.EVoting = false);

        var resultAfter = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, CountingCircleMockedData.GuidGossau, tenantId: tenantId);
        resultAfter.Where(x => exportTemplateIds.Contains(x.ExportTemplateId)).Should().BeEmpty();
    }

    [Fact]
    public async Task PoliticalBusinessEVotingShouldFilter()
    {
        var tenantId = SecureConnectTestDefaults.MockedTenantStGallen.Id;

        var exportTemplateIds = new List<string>
        {
            AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungPdfMajorityElectionTemplates.EndResultEVotingProtocol.Key,
                    tenantId,
                    politicalBusinessId: MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id)
                .ToString(),
            AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungPdfProportionalElectionTemplates.EndResultListUnionsEVoting.Key,
                    tenantId,
                    politicalBusinessId: ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id)
                .ToString(),
            AusmittlungUuidV5.BuildExportTemplate(
                    AusmittlungPdfProportionalElectionTemplates.ListCandidateEndResultsEVoting.Key,
                    tenantId,
                    politicalBusinessId: ProportionalElectionMockedData.StGallenProportionalElectionInContestStGallen.Id)
                .ToString(),
        };

        await ModifyDbEntities<SimplePoliticalBusiness>(
            x => x.PoliticalBusinessType == PoliticalBusinessType.MajorityElection || x.PoliticalBusinessType == PoliticalBusinessType.ProportionalElection,
            pb => pb.EndResultFinalized = true);

        var resultBefore = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);
        resultBefore.Where(x => exportTemplateIds.Contains(x.ExportTemplateId)).Should().NotBeEmpty();

        await ModifyDbEntities<Contest>(
            x => x.Id == Guid.Parse(ContestMockedData.IdStGallenEvoting),
            x => x.EVoting = false);

        var resultAfter = await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null);
        resultAfter.Where(x => exportTemplateIds.Contains(x.ExportTemplateId)).Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringElectionAdminForUnionDoubleProportionalProtocols()
    {
        await RunOnDb(async db =>
        {
            db.ProportionalElectionUnions.Add(new()
            {
                Id = Guid.Parse("ac4955b7-fc32-4688-8035-c448b33a4c01"),
                ContestId = ContestMockedData.GuidStGallenEvoting,
                Description = "Ktratswahl",
                SecureConnectId = SecureConnectTestDefaults.MockedTenantStGallen.Id,
                ProportionalElectionUnionEntries = new List<ProportionalElectionUnionEntry>
                {
                    new() { ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen) },
                },
            });
            await db.SaveChangesAsync();
        });

        await ModifyDbEntities<ProportionalElection>(
            p => p.Id == Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen),
            p => p.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportionalNDois5DoiOr3TotQuorum);

        var result = (await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null))
            .Where(x => x.EntityDescription == "Ktratswahl");
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringElectionAdminForElectionDoubleProportionalProtocols()
    {
        var electionGuid = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen);

        await ModifyDbEntities<ProportionalElection>(
            p => p.Id == electionGuid,
            p => p.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        await ModifyDbEntities<SimplePoliticalBusiness>(
            p => p.Id == electionGuid,
            p => p.EndResultFinalized = true);

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
                AusmittlungPdfProportionalElectionTemplates.EndResultDoubleProportional.Key,
                SecureConnectTestDefaults.MockedTenantStGallen.Id,
                politicalBusinessId: electionGuid)
            .ToString();

        (await FetchExportTemplates(ContestMockedData.GuidStGallenEvoting, null))
            .Any(x => x.ExportTemplateId == exportTemplateId)
            .Should()
            .BeTrue();
    }

    private async Task<List<DataExportTemplate>> FetchExportTemplates(Guid contestId, Guid? countingCircleId = null, ExportFileFormat[]? formats = null, string? tenantId = null)
    {
        tenantId ??= SecureConnectTestDefaults.MockedTenantStGallen.Id;

        return await RunScoped<IServiceProvider, List<DataExportTemplate>>(async sp =>
        {
            var languageService = sp.GetRequiredService<LanguageService>();
            languageService.SetLanguage(Languages.German);

            var permissionProvider = sp.GetRequiredService<IPermissionProvider>();
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", "test", tenantId, null, permissionProvider.GetPermissionsForRoles(new[] { RolesMockedData.ReportExporterApi }));

            var templates = await sp.GetRequiredService<ResultExportTemplateReader>()
                .FetchExportTemplates(contestId, countingCircleId, formats?.ToHashSet());

            return sp.GetRequiredService<IMapper>().Map<List<DataExportTemplate>>(templates);
        });
    }
}
