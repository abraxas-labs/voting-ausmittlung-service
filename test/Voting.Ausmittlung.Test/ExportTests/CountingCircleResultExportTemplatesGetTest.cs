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
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ExportTests;

public class CountingCircleResultExportTemplatesGetTest : BaseTest<ExportService.ExportServiceClient>
{
    private const string PbIdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";

    public CountingCircleResultExportTemplatesGetTest(TestApplicationFactory factory)
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
    public async Task TestShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetCountingCircleResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldIgnoreDisabledExportKey()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisabledExportTemplateKeys.Add(AusmittlungPdfMajorityElectionTemplates.CountingCircleProtocol.Key);
            var response = await ErfassungCreatorClient.GetCountingCircleResultExportTemplatesAsync(NewValidRequest());
            response.Templates.Any(x => x.Key == AusmittlungPdfMajorityElectionTemplates.CountingCircleProtocol.Key).Should().BeFalse();
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
            var response = await ErfassungCreatorClient.GetCountingCircleResultExportTemplatesAsync(NewValidRequest());
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
        var response = await ErfassungElectionAdminClient.GetCountingCircleResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetCountingCircleResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetCountingCircleResultExportTemplatesAsync(
                NewValidRequest(x => x.PoliticalBusinessId = PbIdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetCountingCircleResultExportTemplatesAsync(
                NewValidRequest(x =>
                {
                    x.PoliticalBusinessId = VoteMockedData.IdUzwilVoteInContestBund;
                    x.CountingCircleId = CountingCircleMockedData.IdUzwil;
                })),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .GetCountingCircleResultExportTemplatesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetCountingCircleResultExportTemplatesRequest NewValidRequest(Action<GetCountingCircleResultExportTemplatesRequest>? customizer = null)
    {
        var r = new GetCountingCircleResultExportTemplatesRequest
        {
            PoliticalBusinessId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            PoliticalBusinessType = ProtoModels.PoliticalBusinessType.MajorityElection,
        };
        customizer?.Invoke(r);
        return r;
    }
}
