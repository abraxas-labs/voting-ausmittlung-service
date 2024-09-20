// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultGetOverviewTest : BaseTest<ResultService.ResultServiceClient>
{
    public ResultGetOverviewTest(TestApplicationFactory factory)
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
    public async Task TestShouldReturn()
    {
        var response = await MonitoringElectionAdminClient.GetOverviewAsync(new GetResultOverviewRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        ResetResultIds(response);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnForChild()
    {
        var response = await MonitoringElectionAdminClient.GetOverviewAsync(new GetResultOverviewRequest
        {
            ContestId = ContestMockedData.IdUzwilEvoting,
        });
        ResetResultIds(response);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAllPoliticalBusinesses()
    {
        var response = await StGallenMonitoringElectionAdminClient.GetOverviewAsync(new GetResultOverviewRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        ResetResultIds(response);
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnWithPartialResults()
    {
        await ModifyDbEntities<DomainOfInfluence>(x => x.SnapshotContestId == ContestMockedData.StGallenEvotingUrnengang.Id && x.BasisDomainOfInfluenceId == DomainOfInfluenceMockedData.StGallenStadt.BasisDomainOfInfluenceId, x => x.ViewCountingCirclePartialResults = true);
        var response = await StGallenMonitoringElectionAdminClient.GetOverviewAsync(new GetResultOverviewRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        ResetResultIds(response);
        response.MatchSnapshot();

        response.HasPartialResults.Should().BeTrue();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultService.ResultServiceClient(channel)
            .GetOverviewAsync(new GetResultOverviewRequest
            {
                ContestId = ContestMockedData.IdUzwilEvoting,
            });
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private void ResetResultIds(ProtoModels.ResultOverview response)
    {
        foreach (var ccResult in response.CountingCircleResults)
        {
            foreach (var result in ccResult.Results)
            {
                result.Id = string.Empty;
            }
        }
    }
}
