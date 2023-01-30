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
using Microsoft.EntityFrameworkCore;
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
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ExportTests;

public class PoliticalBusinessResultExportTemplatesGetTest : BaseTest<ExportService.ExportServiceClient>
{
    private const string PbIdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";
    private ExportService.ExportServiceClient _defaultTenantMonitoringAdminClient = null!;

    public PoliticalBusinessResultExportTemplatesGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        _defaultTenantMonitoringAdminClient = new ExportService.ExportServiceClient(
            CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantDefault.Id, TestDefaults.UserId, RolesMockedData.MonitoringElectionAdmin));

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
            async () => await ErfassungCreatorClient.GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAsErfassungElectionAdmin()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldIgnoreDisabledExportKey()
    {
        var config = GetService<PublisherConfig>();
        try
        {
            config.DisabledExportTemplateKeys.Add(AusmittlungXmlProportionalElectionTemplates.Ech0110.Key);
            var response = await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest());
            response.Templates.Any(x => x.Key == AusmittlungXmlProportionalElectionTemplates.Ech0110.Key).Should().BeFalse();
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
            var response = await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest());
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
            async () => await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(
                NewValidRequest(x => x.PoliticalBusinessId = PbIdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldReturnWithFinalizedExportKeys()
    {
        await RunOnDb(async db =>
        {
            var item = await db.ProportionalElectionEndResult
                .AsTracking()
                .FirstAsync(x => x.ProportionalElectionId == Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau));

            item.Finalized = true;

            await db.SaveChangesAsync();
        });
        var response = await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithoutInvalidVotesExportKeys()
    {
        await RunOnDb(async db =>
        {
            var item = await db.MajorityElectionEndResults
                .AsTracking()
                .FirstAsync(x => x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau));

            item.Finalized = true;

            await db.SaveChangesAsync();
        });
        var response = await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(new()
        {
            PoliticalBusinessId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau,
            PoliticalBusinessType = ProtoModels.PoliticalBusinessType.MajorityElection,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithInvalidVotesExportKeys()
    {
        await RunOnDb(async db =>
        {
            var item = await db.MajorityElectionEndResults
                .AsTracking()
                .FirstAsync(x => x.MajorityElectionId == Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau));

            item.Finalized = true;

            var id = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(Guid.Parse(ContestMockedData.IdGossau), Guid.Parse(DomainOfInfluenceMockedData.IdGossau));
            var doi = await db.DomainOfInfluences
                .AsTracking()
                .FirstAsync(x => x.Id == id);

            doi.CantonDefaults.MajorityElectionInvalidVotes = true;

            await db.SaveChangesAsync();
        });
        var response = await MonitoringElectionAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(new()
        {
            PoliticalBusinessId = MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau,
            PoliticalBusinessType = ProtoModels.PoliticalBusinessType.MajorityElection,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowForeignContest()
    {
        await AssertStatus(
            async () => await _defaultTenantMonitoringAdminClient.GetPoliticalBusinessResultExportTemplatesAsync(
                NewValidRequest(x =>
                {
                    x.PoliticalBusinessId = VoteMockedData.IdKircheVoteInContestKircheWithoutChilds;
                    x.PoliticalBusinessType = ProtoModels.PoliticalBusinessType.Vote;
                })),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ExportService.ExportServiceClient(channel)
            .GetPoliticalBusinessResultExportTemplatesAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetPoliticalBusinessResultExportTemplatesRequest NewValidRequest(Action<GetPoliticalBusinessResultExportTemplatesRequest>? customizer = null)
    {
        var r = new GetPoliticalBusinessResultExportTemplatesRequest
        {
            PoliticalBusinessId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau,
            PoliticalBusinessType = ProtoModels.PoliticalBusinessType.ProportionalElection,
        };
        customizer?.Invoke(r);
        return r;
    }
}
