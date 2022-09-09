// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultEnterCountOfVotersTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public MajorityElectionResultEnterCountOfVotersTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultCountOfVotersEntered>().MatchSnapshot();

        await RunEvents<MajorityElectionResultCountOfVotersEntered>(false);

        await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultCountOfVotersEntered>();
        });
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r => r.ElectionResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r => r.ElectionResultId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowNull()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r => r.CountOfVoters = null)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r =>
                    r.ElectionResultId = MajorityElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAlreadySubmitted()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultCountOfVotersEntered
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                CountOfVoters = new PoliticalBusinessCountOfVotersEventData
                {
                    ConventionalReceivedBallots = 10000,
                    ConventionalAccountedBallots = 2000,
                    ConventionalBlankBallots = 1000,
                    ConventionalInvalidBallots = 500,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var result = await RunOnDb(db => db.MajorityElectionResults.FirstAsync(x =>
            x.Id == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund)));
        result.MatchSnapshot(x => x.CountingCircleId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .EnterCountOfVotersAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private EnterMajorityElectionCountOfVotersRequest NewValidRequest(
        Action<EnterMajorityElectionCountOfVotersRequest>? customizer = null)
    {
        var r = new EnterMajorityElectionCountOfVotersRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
            {
                ConventionalReceivedBallots = 10000,
                ConventionalAccountedBallots = 2000,
                ConventionalBlankBallots = 1000,
                ConventionalInvalidBallots = 500,
            },
        };
        customizer?.Invoke(r);
        return r;
    }
}
