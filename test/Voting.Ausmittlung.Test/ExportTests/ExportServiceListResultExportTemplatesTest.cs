// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

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
            var response = await client.ListDataExportTemplatesAsync(NewValidRequest());
            response.MatchSnapshot();
        }
    }

    [Fact]
    public async Task ShouldWorkAsMonitoringElectionAdminWithoutCountingCircleId()
    {
        var response = await MonitoringElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty));
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldThrowAsErfassungElectionAdminWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ShouldThrowAsErfassungCreatorWithoutCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.ListDataExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task AllDisabledShouldReturnEmptyTemplates()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisableAllExports = true;
            var response = await MonitoringElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest());
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
            var responseBefore = await MonitoringElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest());
            config.DisabledExportTemplateKeys.Add(key);
            var responseAfter = await MonitoringElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest());

            responseAfter.Templates.Count.Should().BeLessThan(responseBefore.Templates.Count);
        }
        finally
        {
            GetService<PublisherConfig>().DisabledExportTemplateKeys.Clear();
        }
    }

    [Fact]
    public async Task ActiveContestInMonitoringShouldIncludeActivityProtocol()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, Data.Models.ContestState.Active);
        var response = await MonitoringElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty));

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungCsvContestTemplates.ActivityProtocol.Key,
            CountingCircleMockedData.StGallen.ResponsibleAuthority.SecureConnectId).ToString();

        response.Templates.Should().Contain(x => x.ExportTemplateId == exportTemplateId);
    }

    [Fact]
    public async Task ActiveContestInErfassungShouldFilterActivityProtocol()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, Data.Models.ContestState.Active);
        var response = await ErfassungElectionAdminClient.ListDataExportTemplatesAsync(NewValidRequest());

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungCsvContestTemplates.ActivityProtocol.Key,
            CountingCircleMockedData.StGallen.ResponsibleAuthority.SecureConnectId).ToString();

        response.Templates.Should().NotContain(x => x.ExportTemplateId == exportTemplateId);
    }

    [Fact]
    public async Task ActiveContestInMonitoringNotContestManagerShouldFilterActivityProtocol()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, Data.Models.ContestState.Active);
        var client = CreateServiceWithTenant(SecureConnectTestDefaults.MockedTenantGossau.Id, RolesMockedData.MonitoringElectionAdmin);
        var response = await client.ListDataExportTemplatesAsync(NewValidRequest(x => x.CountingCircleId = string.Empty));

        var exportTemplateId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungCsvContestTemplates.ActivityProtocol.Key,
            CountingCircleMockedData.StGallen.ResponsibleAuthority.SecureConnectId).ToString();

        response.Templates.Should().NotContain(x => x.ExportTemplateId == exportTemplateId);
    }

    [Fact]
    public async Task ShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.ListDataExportTemplatesAsync(new() { ContestId = ContestMockedData.IdGossau }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .ListDataExportTemplatesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private ListDataExportTemplatesRequest NewValidRequest(Action<ListDataExportTemplatesRequest>? reqCustomizer = null)
    {
        var req = new ListDataExportTemplatesRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        };
        reqCustomizer?.Invoke(req);
        return req;
    }
}
