// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ContestTests;

public class ContestGetTest : BaseTest<ContestService.ContestServiceClient>
{
    private const string IdNotFound = "eae2cfaf-c787-48b9-a108-c975b0addddd";
    private const string IdInvalid = "eae2xxxx";

    public ContestGetTest(TestApplicationFactory factory)
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
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ErfassungElectionAdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdUzwilEVoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringAdminShouldReturnWithoutForeignPoliticalBusinesses()
    {
        var response = await MonitoringElectionAdminClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdUzwilEVoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringAdminWithContestsWithoutActivePoliticalBusinessesShouldThrow()
    {
        var contestId = ContestMockedData.GuidUzwilEvoting;

        await RunOnDb(async db =>
        {
            var pbs = await db.SimplePoliticalBusinesses
                .AsTracking()
                .Where(pb => pb.ContestId == contestId)
                .ToListAsync();

            foreach (var pb in pbs)
            {
                pb.Active = false;
            }

            await db.SaveChangesAsync();
        });

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetContestRequest
            {
                Id = contestId.ToString(),
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestAsCreatorShouldReturnOk()
    {
        var response = await ErfassungCreatorClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdUzwilEVoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestParentContestShouldReturnOk()
    {
        var response = await ErfassungCreatorClient.GetAsync(new GetContestRequest
        {
            Id = ContestMockedData.IdStGallenEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsErfassungAdminShouldThrowWithoutAnyAccessiblePoliticalBusiness()
    {
        var contestId = ContestMockedData.GuidBundesurnengang;

        await RunOnDb(db => db.SimpleCountingCircleResults
            .Where(r => r.CountingCircle!.SnapshotContestId == contestId && r.CountingCircle.BasisCountingCircleId != CountingCircleMockedData.StGallen.Id)
            .ExecuteDeleteAsync());
        await ModifyDbEntities<Data.Models.SimpleCountingCircleResult>(
            x => x.CountingCircle!.SnapshotContestId == contestId && x.CountingCircle.BasisCountingCircleId == CountingCircleMockedData.StGallen.Id,
            x => x.CountingCircleId = CountingCircleMockedData.GuidUzwilKirche);

        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetContestRequest
            {
                Id = contestId.ToString(),
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestAsMonitoringAdminShouldReturnWithoutAnyOwnedPoliticalBusiness()
    {
        var contestId = ContestMockedData.GuidStGallenEvoting;

        await ModifyDbEntities<Data.Models.SimplePoliticalBusiness>(
            x => x.ContestId == contestId,
            x => x.DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGenf));

        // update the current user to the contest owner
        await ModifyDbEntities<Data.Models.DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            x => x.SecureConnectId = SecureConnectTestDefaults.MockedTenantUzwil.Id);

        var response = await MonitoringElectionAdminClient.GetAsync(new GetContestRequest
        {
            Id = contestId.ToString(),
        });

        response.Should().NotBeNull();
    }

    [Fact]
    public async Task TestNotVisibleContestShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetAsync(new GetContestRequest
            {
                Id = ContestMockedData.IdKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetAsync(new GetContestRequest
            {
                Id = ContestMockedData.IdKirche,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestInvalidGuidShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetAsync(new GetContestRequest
            {
                Id = IdInvalid,
            }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestNotFound()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetAsync(new GetContestRequest
            {
                Id = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .GetAsync(new GetContestRequest
            {
                Id = ContestMockedData.IdUzwilEVoting,
            });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungRestrictedBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }
}
