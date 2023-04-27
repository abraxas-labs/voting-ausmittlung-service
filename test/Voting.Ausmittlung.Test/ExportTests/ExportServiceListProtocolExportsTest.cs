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
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class ExportServiceListProtocolExportsTest : BaseTest<ExportService.ExportServiceClient>
{
    public ExportServiceListProtocolExportsTest(TestApplicationFactory factory)
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
            var response = await client.ListProtocolExportsAsync(NewValidRequest());
            response.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringElectionAdminWithoutCountingCircleId()
    {
        var response = await MonitoringElectionAdminClient.ListProtocolExportsAsync(NewValidRequest(x => x.CountingCircleId = string.Empty));
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowAsErfassungElectionAdminWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ListProtocolExportsAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowAsErfassungCreatorWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.ListProtocolExportsAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task AllDisabledShouldReturnEmptyTemplates()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisableAllExports = true;
            var response = await MonitoringElectionAdminClient.ListProtocolExportsAsync(NewValidRequest());
            response.ProtocolExports_.Should().BeEmpty();
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
            var responseBefore = await MonitoringElectionAdminClient.ListProtocolExportsAsync(NewValidRequest());
            config.DisabledExportTemplateKeys.Add(key);
            var responseAfter = await MonitoringElectionAdminClient.ListProtocolExportsAsync(NewValidRequest());

            responseBefore.ProtocolExports_.Count.Should().BeGreaterThan(0);
            responseAfter.ProtocolExports_.Count.Should().BeLessThan(responseBefore.ProtocolExports_.Count);
        }
        finally
        {
            GetService<PublisherConfig>().DisabledExportTemplateKeys.Clear();
        }
    }

    [Fact]
    public async Task EndResultFinalizationShouldFilterPdfs()
    {
        var req = NewValidRequest(x => x.CountingCircleId = string.Empty);

        await ModifyDbEntities<SimplePoliticalBusiness>(_ => true, pb => pb.EndResultFinalized = false);
        var responseBefore = await MonitoringElectionAdminClient.ListProtocolExportsAsync(req);

        await ModifyDbEntities<SimplePoliticalBusiness>(_ => true, pb => pb.EndResultFinalized = true);
        var responseAfter = await MonitoringElectionAdminClient.ListProtocolExportsAsync(req);

        responseAfter.ProtocolExports_.Count.Should().BeGreaterThan(responseBefore.ProtocolExports_.Count);
    }

    [Fact]
    public async Task InvalidVotesShouldFilter()
    {
        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfMajorityElectionTemplates.EndResultDetailWithoutEmptyAndInvalidVotesProtocol.Key,
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            politicalBusinessId: MajorityElectionMockedData.StGallenMajorityElectionInContestStGallen.Id)
            .ToString();

        var req = NewValidRequest(x => x.CountingCircleId = string.Empty);
        await ModifyDbEntities<SimplePoliticalBusiness>(
            x => x.PoliticalBusinessType == PoliticalBusinessType.MajorityElection,
            pb => pb.EndResultFinalized = true);
        await ModifyDbEntities<DomainOfInfluence>(_ => true, x => x.CantonDefaults.MajorityElectionInvalidVotes = true);
        var response = await MonitoringElectionAdminClient.ListProtocolExportsAsync(req);

        response.ProtocolExports_.Where(x => x.ExportTemplateId == exportTemplateId).Should().NotBeEmpty();
        await ModifyDbEntities<DomainOfInfluence>(_ => true, x => x.CantonDefaults.MajorityElectionInvalidVotes = false);
        response = await MonitoringElectionAdminClient.ListProtocolExportsAsync(req);

        response.ProtocolExports_.Where(x => x.ExportTemplateId == exportTemplateId).Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ListProtocolExportsAsync(new() { ContestId = ContestMockedData.IdGossau }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .ListProtocolExportsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
        => new[] { NoRole };

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private ListProtocolExportsRequest NewValidRequest(Action<ListProtocolExportsRequest>? reqCustomizer = null)
    {
        var req = new ListProtocolExportsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        };
        reqCustomizer?.Invoke(req);
        return req;
    }
}