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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultPrepareSubmissionFinishedAndAuditedTentativelyTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultPrepareSubmissionFinishedAndAuditedTentativelyTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        var response = await ErfassungElectionAdminClient.PrepareSubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
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
                new ProportionalElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest
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
                new ProportionalElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .PrepareSubmissionFinishedAndAuditedTentativelyAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private ProportionalElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest NewValidRequest()
    {
        return new ProportionalElectionResultPrepareSubmissionFinishedAndAuditedTentativelyRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        };
    }
}
