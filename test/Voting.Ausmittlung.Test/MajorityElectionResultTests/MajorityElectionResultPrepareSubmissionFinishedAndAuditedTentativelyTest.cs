// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultPrepareSubmissionFinishedAndAuditedTentativelyTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultPrepareSubmissionFinishedAndAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Make sure that St. Gallen is not the owner of the canton, as that would make the endpoints not require 2FA
        // This never happens from a business point of view
        await ModifyDbEntities<CantonSettings>(
            x => x.Canton == DomainOfInfluenceCanton.Sg,
            x => x.SecureConnectId = "someone-else");
    }

    [Fact]
    public async Task TestShouldReturn()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        var response = await BundErfassungElectionAdminClient.PrepareSubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
        response.Id.Should().NotBeEmpty();
        response.Code.Should().NotBeEmpty();
        response.QrCode.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.PrepareSubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.PrepareSubmissionFinishedAndAuditedTentativelyAsync(
                new MajorityElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .PrepareSubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private MajorityElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest()
    {
        return new MajorityElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        };
    }
}
