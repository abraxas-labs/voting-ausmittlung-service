// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.DomainOfInfluenceTests;

public class DomainOfInfluenceGetCantonDefaultsTest : BaseTest<DomainOfInfluenceService.DomainOfInfluenceServiceClient>
{
    public DomainOfInfluenceGetCantonDefaultsTest(TestApplicationFactory factory)
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
        var response = await ErfassungElectionAdminClient.GetCantonDefaultsAsync(new()
        {
            ContestId = ContestMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringAdminShouldReturnOk()
    {
        var response = await MonitoringElectionAdminClient.GetCantonDefaultsAsync(new()
        {
            ContestId = ContestMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCreatorShouldReturnOk()
    {
        var response = await ErfassungCreatorClient.GetCantonDefaultsAsync(new()
        {
            ContestId = ContestMockedData.IdGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminWithPoliticalBusinessIdShouldReturnOk()
    {
        var response = await ErfassungElectionAdminClient.GetCantonDefaultsAsync(new()
        {
            PoliticalBusinessId = VoteMockedData.IdGossauVoteInContestGossau,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminWithResultIdShouldReturnOk()
    {
        var response = await ErfassungElectionAdminClient.GetCantonDefaultsAsync(new()
        {
            CountingCircleResultId = VoteResultMockedData.IdGossauVoteInContestGossauResult,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetCantonDefaultsAsync(new()
            {
                ContestId = ContestMockedData.IdUzwilEvoting,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestWithoutIdShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.GetCantonDefaultsAsync(new()),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new DomainOfInfluenceService.DomainOfInfluenceServiceClient(channel)
            .GetCantonDefaultsAsync(new()
            {
                ContestId = ContestMockedData.IdGossau,
            });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }
}
