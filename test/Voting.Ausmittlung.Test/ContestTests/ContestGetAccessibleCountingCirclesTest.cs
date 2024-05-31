// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ContestTests;

public class ContestGetAccessibleCountingCirclesTest : BaseTest<ContestService.ContestServiceClient>
{
    public ContestGetAccessibleCountingCirclesTest(TestApplicationFactory factory)
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
        var response = await ErfassungElectionAdminClient.GetAccessibleCountingCirclesAsync(new GetAccessibleCountingCirclesRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringAdminShouldReturnWithoutForeignPoliticalBusinesses()
    {
        var response = await MonitoringElectionAdminClient.GetAccessibleCountingCirclesAsync(new GetAccessibleCountingCirclesRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCreatorShouldReturnOk()
    {
        var response = await ErfassungCreatorClient.GetAccessibleCountingCirclesAsync(new GetAccessibleCountingCirclesRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestParentContestShouldReturnOk()
    {
        var response = await ErfassungCreatorClient.GetAccessibleCountingCirclesAsync(new GetAccessibleCountingCirclesRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestTenantWithMultipleDomainOfInfluenceShouldReturnOk()
    {
        var response = await StGallenErfassungElectionAdminClient.GetAccessibleCountingCirclesAsync(new GetAccessibleCountingCirclesRequest
        {
            ContestId = ContestMockedData.IdStGallenStadt,
        });
        response.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .GetAccessibleCountingCirclesAsync(new GetAccessibleCountingCirclesRequest
            {
                ContestId = ContestMockedData.IdUzwilEvoting,
            });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }
}
