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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.ExportTests;

public class MultiplePoliticalBusinessesCountingCircleResultExportTemplatesGetTest : BaseTest<ExportService.ExportServiceClient>
{
    private const string ContestIdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";

    public MultiplePoliticalBusinessesCountingCircleResultExportTemplatesGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);

        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldIgnoreDisabledExportKey()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisabledExportTemplateKeys.Add(AusmittlungPdfVoteTemplates.ResultProtocol.Key);
            var response = await ErfassungCreatorClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(NewValidRequest());
            response.Templates.Any(x => x.Key == AusmittlungPdfVoteTemplates.ResultProtocol.Key).Should().BeFalse();
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
            var response = await ErfassungCreatorClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(NewValidRequest());
            response.Templates.Should().BeEmpty();
        }
        finally
        {
            config.DisableAllExports = false;
        }
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(
                NewValidRequest(x => x.ContestId = ContestIdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(
                NewValidRequest(x => x.ContestId = ContestMockedData.IdThurgauNoPoliticalBusinesses)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowForeignCountingCircle()
    {
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(
                NewValidRequest(x => x.CountingCircleId = CountingCircleMockedData.IdRorschach)),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest NewValidRequest(Action<GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest>? customizer = null)
    {
        var r = new GetMultiplePoliticalBusinessesCountingCircleResultExportTemplatesRequest()
        {
            ContestId = ContestMockedData.IdGossau,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        };
        customizer?.Invoke(r);
        return r;
    }
}
