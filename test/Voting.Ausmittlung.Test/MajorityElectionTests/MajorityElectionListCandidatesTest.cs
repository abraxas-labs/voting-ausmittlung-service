// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionTests;

public class MajorityElectionListCandidatesTest : BaseTest<MajorityElectionService.MajorityElectionServiceClient>
{
    public MajorityElectionListCandidatesTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestAsElectionAdminShouldReturnOk()
    {
        var response = await ErfassungElectionAdminClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsElectionAdminWithSecondaryShouldReturnOk()
    {
        var response = await ErfassungElectionAdminClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            IncludeCandidatesOfSecondaryElection = true,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsCreatorShouldReturnOk()
    {
        var response = await ErfassungCreatorClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestAsMonitoringElectionAdminShouldReturnOk()
    {
        var response = await MonitoringElectionAdminClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestWithoutSecondaryElectionShouldReturnOk()
    {
        var response = await ErfassungElectionAdminClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
        {
            ElectionId = MajorityElectionMockedData.IdBundMajorityElectionInContestStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestOtherTenantShouldThrow()
    {
        await AssertStatus(
            async () => await ErfassungCreatorClient.ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
            {
                ElectionId = MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche,
            }),
            StatusCode.PermissionDenied);
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionService.MajorityElectionServiceClient(channel)
            .ListCandidatesAsync(new ListMajorityElectionCandidatesRequest
            {
                ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            });
    }

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
