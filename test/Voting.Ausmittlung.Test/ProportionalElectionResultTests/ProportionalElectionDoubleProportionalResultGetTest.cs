// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionDpResultGetTest : ProportionalElectionEndResultBaseTest
{
    public ProportionalElectionDpResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await SeedElectionAndFinishSubmissions();

        await ModifyDbEntities<ProportionalElection>(
            x => x.Id == ProportionalElectionEndResultMockedData.ElectionGuid,
            x => x.MandateAlgorithm = ProportionalElectionMandateAlgorithm.DoubleProportional1Doi0DoiQuorum);

        await SetAllAuditedTentatively();
    }

    [Fact]
    public async Task TestShouldReturnAsOwnerMonitoringElection()
    {
        var endResult = await MonitoringElectionAdminClient.GetDoubleProportionalResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowForeignMonitoringElection()
    {
        var client = CreateServiceWithTenant(SecureConnectTestDefaults.MockedTenantGossau.Id, RolesMockedData.MonitoringElectionAdmin);
        await AssertStatus(
            async () => await client.GetDoubleProportionalResultAsync(NewValidRequest()),
            StatusCode.NotFound);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetDoubleProportionalResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private GetProportionalElectionDoubleProportionalResultRequest NewValidRequest()
    {
        return new() { ProportionalElectionId = ProportionalElectionEndResultMockedData.ElectionId };
    }
}
