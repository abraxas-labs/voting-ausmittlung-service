// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionEndResultPrepareFinalizeTest : ProportionalElectionEndResultBaseTest
{
    public ProportionalElectionEndResultPrepareFinalizeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();
        await SetAllAuditedTentatively();
    }

    [Fact]
    public async Task ShouldWork()
    {
        var response = await MonitoringElectionAdminClient.PrepareFinalizeEndResultAsync(NewValidRequest());
        response.Id.Should().NotBeEmpty();
        response.Code.Should().NotBeEmpty();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .PrepareFinalizeEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return NoRole;
    }

    private PrepareFinalizeProportionalElectionEndResultRequest NewValidRequest()
    {
        return new PrepareFinalizeProportionalElectionEndResultRequest
        {
            ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId,
        };
    }
}
