// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Net.Client;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ContestTests;

public class ContestListPoliticalBusinessUnionsTest : BaseTest<ContestService.ContestServiceClient>
{
    public ContestListPoliticalBusinessUnionsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await MajorityElectionMockedData.Seed(RunScoped);
        await ProportionalElectionUnionMockedData.Seed(RunScoped);
        await MajorityElectionUnionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) => permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task ShouldWorkAsElectionAdmin()
    {
        var result = await MonitoringElectionAdminClient.ListPoliticalBusinessUnionsAsync(new ListPoliticalBusinessUnionsRequest
        {
            ContestId = ContestMockedData.IdStGallenEvoting,
        });
        result.ShouldMatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ContestService.ContestServiceClient(channel)
            .ListPoliticalBusinessUnionsAsync(new ListPoliticalBusinessUnionsRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
            });
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return NoRole;
    }
}
