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
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionEndResultGetTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionEndResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminInitial()
    {
        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterSubmissionDone()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminReadyForCorrection()
    {
        await RunToState(CountingCircleResultState.ReadyForCorrection);
        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminAfterCorrection()
    {
        await RunToState(CountingCircleResultState.CorrectionDone);
        var endResult = await MonitoringElectionAdminClient.GetEndResultAsync(NewValidRequest());
        endResult.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetEndResultAsync(
                new GetMajorityElectionEndResultRequest
                {
                    MajorityElectionId = IdNotFound,
                }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await MonitoringElectionAdminClient.GetEndResultAsync(
                new GetMajorityElectionEndResultRequest
                {
                    MajorityElectionId = IdBadFormat,
                }),
            StatusCode.InvalidArgument);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .GetEndResultAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await MajorityElectionResultMockedData.InjectCandidateResults(RunScoped);
    }

    private GetMajorityElectionEndResultRequest NewValidRequest()
    {
        return new GetMajorityElectionEndResultRequest
        {
            MajorityElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
        };
    }
}
