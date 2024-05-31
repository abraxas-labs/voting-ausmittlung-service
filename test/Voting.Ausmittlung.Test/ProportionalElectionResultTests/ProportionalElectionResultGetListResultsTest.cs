// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultGetListResultsTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultGetListResultsTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var result = await ErfassungElectionAdminClient.GetListResultsAsync(NewValidRequest());

        foreach (var listResult in result.ListResults)
        {
            listResult.Id = string.Empty;
            foreach (var candidateResult in listResult.CandidateResults)
            {
                candidateResult.Id = string.Empty;
            }
        }

        result.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var result = await MonitoringElectionAdminClient.GetListResultsAsync(NewValidRequest());

        foreach (var listResult in result.ListResults)
        {
            listResult.Id = string.Empty;
            foreach (var candidateResult in listResult.CandidateResults)
            {
                candidateResult.Id = string.Empty;
            }
        }

        result.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetListResultsAsync(
                new GetProportionalElectionListResultsRequest
                {
                    ElectionResultId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetListResultsAsync(
                new GetProportionalElectionListResultsRequest
                {
                    ElectionResultId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(
                new EnterProportionalElectionUnmodifiedListResultsRequest
                {
                    ElectionResultId = ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
                }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetListResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    private GetProportionalElectionListResultsRequest NewValidRequest()
    {
        return new GetProportionalElectionListResultsRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        };
    }
}
