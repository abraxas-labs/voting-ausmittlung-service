// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportServiceListResultExportTemplatesTest : BaseTest<ExportService.ExportServiceClient>
{
    public ExportServiceListResultExportTemplatesTest(TestApplicationFactory factory)
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
        await MajorityElectionUnionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ShouldWorkAsAllRolesAndReturnTheSame()
    {
        var roleClientsToTest = new[]
        {
            MonitoringElectionAdminClient,
            ErfassungElectionAdminClient,
            ErfassungCreatorClient,
        };

        foreach (var client in roleClientsToTest)
        {
            var response = await client.ListExportTemplatesAsync(NewValidRequest());
            response.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringElectionAdminWithoutCountingCircleId()
    {
        var response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty));
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowAsErfassungElectionAdminWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ListExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowAsErfassungCreatorWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.ListExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task AllDisabledShouldReturnEmptyTemplates()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisableAllExports = true;
            var response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(NewValidRequest());
            response.Templates.Should().BeEmpty();
        }
        finally
        {
            config.DisableAllExports = false;
        }
    }

    [Fact]
    public async Task DisabledExportKeyShouldIgnore()
    {
        var key = AusmittlungCsvProportionalElectionTemplates.CandidatesAlphabetical.Key;
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisabledExportTemplateKeys.Add(key);
            var response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(NewValidRequest());
            response.Templates.Any(x => x.Key == key).Should().BeFalse();
        }
        finally
        {
            GetService<PublisherConfig>().DisabledExportTemplateKeys.Clear();
        }
    }

    [Fact]
    public async Task EndResultFinalizationShouldFilterPdfs()
    {
        var req = NewValidRequest(x =>
        {
            x.CountingCircleId = string.Empty;
            x.Formats.Clear();
            x.Formats.Add(ProtoModels.ExportFileFormat.Pdf);
        });

        await ModifyDbEntities<SimplePoliticalBusiness>(_ => true, pb => pb.EndResultFinalized = false);
        var response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(req);
        response.Templates.Where(x => x.ResultType == ProtoModels.ExportResultType.PoliticalBusinessResult).Should().BeEmpty();

        await ModifyDbEntities<SimplePoliticalBusiness>(_ => true, pb => pb.EndResultFinalized = true);
        response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(req);
        response.Templates.Where(x => x.ResultType == ProtoModels.ExportResultType.PoliticalBusinessResult).Should().NotBeEmpty();
    }

    [Fact]
    public async Task InvalidVotesShouldFilter()
    {
        var key = AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key;
        var req = NewValidRequest(x =>
        {
            x.CountingCircleId = string.Empty;
            x.Formats.Clear();
            x.Formats.Add(ProtoModels.ExportFileFormat.Pdf);
        });
        await ModifyDbEntities<SimplePoliticalBusiness>(
            x => x.PoliticalBusinessType == PoliticalBusinessType.MajorityElection,
            pb => pb.EndResultFinalized = true);
        await ModifyDbEntities<DomainOfInfluence>(_ => true, x => x.CantonDefaults.MajorityElectionInvalidVotes = true);
        var response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(req);
        response.Templates.Where(x => x.Key == key).Should().NotBeEmpty();

        await ModifyDbEntities<DomainOfInfluence>(_ => true, x => x.CantonDefaults.MajorityElectionInvalidVotes = false);
        response = await MonitoringElectionAdminClient.ListExportTemplatesAsync(req);
        response.Templates.Where(x => x.Key == key).Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ListExportTemplatesAsync(new() { ContestId = ContestMockedData.IdGossau }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .ListExportTemplatesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
        => new[] { NoRole };

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private ListResultExportTemplatesRequest NewValidRequest(Action<ListResultExportTemplatesRequest>? reqCustomizer = null)
    {
        var req = new ListResultExportTemplatesRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
            Formats =
            {
                ProtoModels.ExportFileFormat.Csv,
                ProtoModels.ExportFileFormat.Pdf,
                ProtoModels.ExportFileFormat.Xml,
            },
        };
        reqCustomizer?.Invoke(req);
        return req;
    }
}
