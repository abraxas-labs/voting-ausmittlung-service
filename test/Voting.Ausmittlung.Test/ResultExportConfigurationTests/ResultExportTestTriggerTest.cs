// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.Mocks;
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
        await StGallenMonitoringElectionAdminClient.TriggerResultExportAsync(NewValidRequest());
        var connector = GetService<DokConnectorMock>();
        var save = await connector.WaitForNextSave();
        save.MatchSnapshot();
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.TriggerResultExportAsync(
                NewValidRequest(r => r.ExportConfigurationId = ExportConfigurationMockedData.IdGossauIntf100)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public Task ShouldThrowDuplicatedPoliticalBusiness()
    {
        return AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.TriggerResultExportAsync(
                NewValidRequest(r => r.PoliticalBusinessIds.Add(r.PoliticalBusinessIds[0]))),
            StatusCode.InvalidArgument,
            "Political business ids have to be unique");
    }

    [Fact]
    public Task ShouldThrowWrongPoliticalBusiness()
    {
        return AssertStatus(
            async () => await StGallenMonitoringElectionAdminClient.TriggerResultExportAsync(
                NewValidRequest(r => r.PoliticalBusinessIds.Add(VoteMockedData.IdUzwilVoteInContestStGallen))),
            StatusCode.InvalidArgument,
            "Political business ids provided without access");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel).TriggerResultExportAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private TriggerResultExportRequest NewValidRequest(
        Action<TriggerResultExportRequest>? customizer = null)
    {
        var req = new TriggerResultExportRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            ExportConfigurationId = ExportConfigurationMockedData.IdStGallenIntf001,
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
