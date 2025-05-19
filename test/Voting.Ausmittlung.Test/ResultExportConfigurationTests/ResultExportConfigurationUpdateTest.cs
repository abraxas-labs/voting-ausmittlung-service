// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultExportConfigurationTests;

public class ResultExportConfigurationUpdateTest : BaseTest<ExportService.ExportServiceClient>
{
    public ResultExportConfigurationUpdateTest(TestApplicationFactory factory)
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
        await MonitoringElectionAdminClient.UpdateResultExportConfigurationAsync(NewValidRequest());
        var ev = EventPublisherMock.GetSinglePublishedEvent<ResultExportConfigurationUpdated>();
        ev.MatchSnapshot();
    }

    [Fact]
    public Task ShouldThrowOtherTenant()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateResultExportConfigurationAsync(
                NewValidRequest(r => r.ExportConfigurationId = ExportConfigurationMockedData.IdGossauIntf100)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public Task ShouldThrowDuplicatedPoliticalBusiness()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateResultExportConfigurationAsync(
                NewValidRequest(r => r.PoliticalBusinessIds.Add(r.PoliticalBusinessIds[0]))),
            StatusCode.InvalidArgument,
            "Political business ids have to be unique");
    }

    [Fact]
    public Task ShouldThrowWrongPoliticalBusiness()
    {
        return AssertStatus(
            async () => await MonitoringElectionAdminClient.UpdateResultExportConfigurationAsync(
                NewValidRequest(r => r.PoliticalBusinessIds.Add(VoteMockedData.IdUzwilVoteInContestStGallen))),
            StatusCode.InvalidArgument,
            "Political business ids provided without access");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(new ResultExportConfigurationUpdated
        {
            ExportConfiguration = new ResultExportConfigurationEventData
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                ExportConfigurationId = ExportConfigurationMockedData.IdStGallenIntf001,
                IntervalMinutes = 10,
                Description = "updated",
                PoliticalBusinessIds =
                {
                    ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
                },
                PoliticalBusinessMetadata =
                {
                    [MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen] = new PoliticalBusinessExportMetadataEventData()
                    {
                        Token = "test-token",
                    },
                },
            },
        });

        var id = AusmittlungUuidV5.BuildResultExportConfiguration(
            Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Guid.Parse(ExportConfigurationMockedData.IdStGallenIntf001));
        var updated = await RunOnDb(db => db.ResultExportConfigurations
            .AsSplitQuery()
            .Include(x => x.PoliticalBusinesses)
            .Include(x => x.PoliticalBusinessMetadata)
            .FirstAsync(x => x.Id == id));

        foreach (var business in updated.PoliticalBusinesses!)
        {
            business.Id = Guid.Empty;
        }

        foreach (var metadata in updated.PoliticalBusinessMetadata!)
        {
            metadata.Id = Guid.Empty;
        }

        updated.DomainOfInfluenceId = Guid.Empty;
        updated.MatchSnapshot();
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel).UpdateResultExportConfigurationAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private UpdateResultExportConfigurationRequest NewValidRequest(
        Action<UpdateResultExportConfigurationRequest>? customizer = null)
    {
        var req = new UpdateResultExportConfigurationRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
            ExportConfigurationId = ExportConfigurationMockedData.IdStGallenIntf001,
            IntervalMinutes = 10,
            PoliticalBusinessIds =
            {
                MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen,
                ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen,
            },
            PoliticalBusinessMetadata =
            {
                [MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen] = new UpdatePoliticalBusinessExportMetadataRequest
                {
                    Token = "test-token",
                },
                [ProportionalElectionMockedData.IdStGallenProportionalElectionInContestStGallen] = new UpdatePoliticalBusinessExportMetadataRequest
                {
                    Token = "test-token2",
                },
            },
        };
        customizer?.Invoke(req);
        return req;
    }
}
