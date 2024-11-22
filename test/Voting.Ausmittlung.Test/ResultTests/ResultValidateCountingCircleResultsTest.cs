// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ResultTests;

public class ResultValidateCountingCircleResultsTest : BaseTest<ResultService.ResultServiceClient>
{
    private static readonly Guid ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting);
    private static readonly Guid CountingCircleId = CountingCircleMockedData.GuidUzwil;

    public ResultValidateCountingCircleResultsTest(TestApplicationFactory factory)
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
    public async Task ShouldReturn()
    {
        // ensure that the tenant is only the cc manager and not the contest manager
        await ModifyDbEntities<DomainOfInfluence>(
            x => x.BasisDomainOfInfluenceId == Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            x => x.SecureConnectId = "random-id");

        var response = await ErfassungElectionAdminClient.ValidateCountingCircleResultsAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task ShouldBeOkAsContestManagerDuringTestingPhase()
    {
        // ensure that the tenant is only the contest manager
        await ModifyDbEntities<Authority>(
            x => x.CountingCircle!.BasisCountingCircleId == CountingCircleId,
            x => x.SecureConnectId = "random-id");

        var response = await ErfassungElectionAdminClient.ValidateCountingCircleResultsAsync(NewValidRequest());
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldThrowIfNotCountingCircleManager()
    {
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.ValidateCountingCircleResultsAsync(NewValidRequest()),
            StatusCode.PermissionDenied,
            "This tenant is not the contest manager or the testing phase has ended and the counting circle does not belong to this tenant");
    }

    [Fact]
    public async Task ShouldThrowIfResultNotFromCountingCircle()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.ValidateCountingCircleResultsAsync(
                NewValidRequest(x => x.CountingCircleResultIds.Add(VoteResultMockedData.IdUzwilVoteInContestStGallenResult))),
            StatusCode.InvalidArgument,
            "Non existing counting circle result id provided");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ResultService.ResultServiceClient(channel)
            .ValidateCountingCircleResultsAsync(NewValidRequest());
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantUzwil.Id, TestDefaults.UserId, roles);

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ValidateCountingCircleResultsRequest NewValidRequest(Action<ValidateCountingCircleResultsRequest>? action = null)
    {
        var request = new ValidateCountingCircleResultsRequest
        {
            ContestId = ContestId.ToString(),
            CountingCircleId = CountingCircleId.ToString(),
            CountingCircleResultIds =
            {
                VoteResultMockedData.IdUzwilVoteInContestUzwilResult,
                ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
                MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
            },
        };

        action?.Invoke(request);
        return request;
    }
}
