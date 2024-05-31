// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.DokConnector.Testing.Service;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultExportConfigurationTests;

public class ResultExportTestTriggerTest : BaseTest<ExportService.ExportServiceClient>
{
    public ResultExportTestTriggerTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await ExportConfigurationMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ShouldWork()
    {
        await MonitoringElectionAdminClient.TriggerResultExportAsync(NewValidRequest());
        var connector = GetService<DokConnectorMock>();
        var save = await connector.NextUpload(TimeSpan.FromSeconds(10));

        // This is a CSV export, so we better use the textual representation as snapshot
        new
        {
            save.FileName,
            save.MessageType,
            Data = Encoding.UTF8.GetString(save.Data),
        }.MatchSnapshot();
    }

    [Fact]
    public async Task ProcessorShouldWork()
    {
        await MonitoringElectionAdminClient.TriggerResultExportAsync(NewValidRequest());

        // await async process
        var connector = GetService<DokConnectorMock>();
        await connector.NextUpload(TimeSpan.FromSeconds(10));
        await Task.Delay(1000);

        var evs = EventPublisherMock.GetPublishedEvents<ResultExportCompleted>();
        await TestEventPublisher.Publish(evs.First());

        var config = await RunOnDb(db => db.ResultExportConfigurations
            .FirstAsync(x => x.ExportConfigurationId == Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf002)
                && x.ContestId == Guid.Parse(ContestMockedData.IdStGallenEvoting)));
        config.MatchSnapshot(x => x.Id);
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.TriggerResultExportAsync(
                NewValidRequest(r => r.ExportConfigurationId = ExportConfigurationMockedData.IdGossauIntf100)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public Task ShouldThrowDuplicatedPoliticalBusiness()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.TriggerResultExportAsync(
                NewValidRequest(r => r.PoliticalBusinessIds.Add(r.PoliticalBusinessIds[0]))),
            StatusCode.InvalidArgument,
            "Political business ids have to be unique");
    }

    [Fact]
    public Task ShouldThrowWrongPoliticalBusiness()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.TriggerResultExportAsync(
                NewValidRequest(r => r.PoliticalBusinessIds.Add(VoteMockedData.IdUzwilVoteInContestStGallen))),
            StatusCode.InvalidArgument,
            "Political business ids provided without access");
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel).TriggerResultExportAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private TriggerResultExportRequest NewValidRequest(
        Action<TriggerResultExportRequest>? customizer = null)
    {
        var req = new TriggerResultExportRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            ExportConfigurationId = ExportConfigurationMockedData.IdStGallenIntf002,
            PoliticalBusinessIds =
            {
                MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            },
        };
        customizer?.Invoke(req);
        return req;
    }
}
