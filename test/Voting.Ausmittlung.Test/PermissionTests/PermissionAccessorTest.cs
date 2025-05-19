// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Services;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Authorization;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.VotingExports.Repository.Ausmittlung;
using Xunit;

namespace Voting.Ausmittlung.Test.PermissionTests;

public class PermissionAccessorTest : BaseIntegrationTest
{
    private PermissionAccessor _accessor = null!;

    public PermissionAccessorTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await VoteMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) => permissionBuilder.RebuildPermissionTree());
        GetService<LanguageService>().SetLanguage("de");
        TrySetFakeAuth(SecureConnectTestDefaults.MockedTenantStGallen.Id, RolesMockedData.ErfassungElectionAdmin);
        _accessor = GetService<PermissionAccessor>();
    }

    [Fact]
    public async Task CanReadCountingCircle()
    {
        _accessor.SetContextIds(CountingCircleMockedData.GuidStGallen, ContestMockedData.GuidStGallenEvoting, false);
        var result = await _accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { BasisCountingCircleId = CountingCircleMockedData.GuidStGallen });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReadOtherCountingCircleWithoutContextId()
    {
        // use a new scope to set other auth info
        await using var scope = GetService<IServiceScopeFactory>().CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<LanguageService>().SetLanguage("de");
        var authStore = scope.ServiceProvider.GetRequiredService<IAuthStore>();
        authStore.SetValues(
            "mock-token",
            "fake",
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            [RolesMockedData.MonitoringElectionAdmin],
            scope.ServiceProvider.GetRequiredService<IPermissionProvider>().GetPermissionsForRoles([RolesMockedData.MonitoringElectionAdmin]));
        var accessor = scope.ServiceProvider.GetRequiredService<PermissionAccessor>();
        accessor.SetContextIds(null, ContestMockedData.GuidStGallenEvoting, false);
        var result = await accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { BasisCountingCircleId = CountingCircleMockedData.GuidUzwil });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CannotReadOtherCountingCircle()
    {
        _accessor.SetContextIds(CountingCircleMockedData.GuidStGallen, ContestMockedData.GuidStGallenEvoting, false);
        var result = await _accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { BasisCountingCircleId = CountingCircleMockedData.GuidUzwilKirche });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadResult()
    {
        _accessor.SetContextIds(CountingCircleMockedData.GuidUzwil, ContestMockedData.GuidStGallenEvoting, false);
        var result = await _accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { PoliticalBusinessResultId = VoteResultMockedData.GuidUzwilVoteInContestStGallenResult });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CannotReadUnrelatedResult()
    {
        _accessor.SetContextIds(CountingCircleMockedData.GuidUzwil, ContestMockedData.GuidStGallenEvoting, false);
        var result = await _accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { PoliticalBusinessResultId = VoteResultMockedData.GuidGossauVoteInContestGossauResult });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanReadProtocol()
    {
        var exportId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfVoteTemplates.EVotingCountingCircleResultProtocol.Key,
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            CountingCircleMockedData.GuidUzwil);
        _accessor.SetContextIds(CountingCircleMockedData.GuidUzwil, ContestMockedData.GuidStGallenEvoting, false);
        var result = await _accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { ProtocolExportId = AusmittlungUuidV5.BuildProtocolExport(ContestMockedData.GuidStGallenEvoting, false, exportId) });
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CannotReadOtherProtocol()
    {
        var exportId = AusmittlungUuidV5.BuildExportTemplate(
            AusmittlungPdfVoteTemplates.EVotingCountingCircleResultProtocol.Key,
            SecureConnectTestDefaults.MockedTenantStGallen.Id,
            CountingCircleMockedData.GuidUzwilKirche);
        _accessor.SetContextIds(CountingCircleMockedData.GuidUzwil, ContestMockedData.GuidStGallenEvoting, false);
        var result = await _accessor.CanRead(new EventProcessedMessage("foo", DateTime.Now) { ProtocolExportId = AusmittlungUuidV5.BuildProtocolExport(ContestMockedData.GuidStGallenEvoting, false, exportId) });
        result.Should().BeFalse();
    }
}
