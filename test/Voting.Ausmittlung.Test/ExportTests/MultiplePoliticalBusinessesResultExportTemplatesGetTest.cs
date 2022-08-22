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
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Configuration;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class MultiplePoliticalBusinessesResultExportTemplatesGetTest : BaseTest<ExportService.ExportServiceClient>
{
    private const string ContestIdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";

    public MultiplePoliticalBusinessesResultExportTemplatesGetTest(TestApplicationFactory factory)
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
    public async Task TestShouldThrowAsErfassungCreator()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungElectionAdmin()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldIgnoreDisabledExportKey()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisabledExportTemplateKeys.Add(AusmittlungWabstiCTemplates.SGGemeinden.Key);
            var response = await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(NewValidRequest());
            response.Templates.Any(x => x.Key == AusmittlungWabstiCTemplates.SGGemeinden.Key).Should().BeFalse();
        }
        finally
        {
            GetService<PublisherConfig>().DisabledExportTemplateKeys.Clear();
        }
    }

    [Fact]
    public async Task TestShouldIgnoreAllIfDisabled()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisableAllExports = true;
            var response = await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(NewValidRequest());
            response.Templates.Should().BeEmpty();
        }
        finally
        {
            config.DisableAllExports = false;
        }
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(
                NewValidRequest(x => x.ContestId = ContestIdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesResultExportTemplatesAsync(
                NewValidRequest(x => x.ContestId = ContestMockedData.IdThurgauNoPoliticalBusinesses)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .GetMultiplePoliticalBusinessesResultExportTemplatesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetMultiplePoliticalBusinessesResultExportTemplatesRequest NewValidRequest(Action<GetMultiplePoliticalBusinessesResultExportTemplatesRequest>? customizer = null)
    {
        var r = new GetMultiplePoliticalBusinessesResultExportTemplatesRequest
        {
            ContestId = ContestMockedData.IdGossau,
        };
        customizer?.Invoke(r);
        return r;
    }
}
