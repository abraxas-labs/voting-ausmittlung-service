// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteEndResultPrepareFinalizeTest : VoteResultBaseTest
{
    public VoteEndResultPrepareFinalizeTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.AuditedTentatively);
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
        await new VoteResultService.VoteResultServiceClient(channel)
            .PrepareFinalizeEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return NoRole;
    }

    private PrepareFinalizeVoteEndResultRequest NewValidRequest()
    {
        return new PrepareFinalizeVoteEndResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
        };
    }
}
