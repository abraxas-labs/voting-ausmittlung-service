// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultEnterCountOfVotersTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultEnterCountOfVotersTest(TestApplicationFactory factory)
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
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCountOfVotersEntered>().MatchSnapshot();

        await RunEvents<ProportionalElectionResultCountOfVotersEntered>(false);

        await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultCountOfVotersEntered>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await StGallenErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultCountOfVotersEntered>().MatchSnapshot();

        await RunEvents<ProportionalElectionResultCountOfVotersEntered>(false);

        await StGallenErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
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
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r =>
                    r.ElectionResultId = ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
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
            new ProportionalElectionResultCountOfVotersEntered
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                CountOfVoters = new PoliticalBusinessCountOfVotersEventData
                {
                    ConventionalReceivedBallots = 10000,
                    ConventionalAccountedBallots = 2000,
                    ConventionalBlankBallots = 1000,
                    ConventionalInvalidBallots = 500,
                },
                EventInfo = GetMockedEventInfo(),
            });
        var result = await RunOnDb(db => db.ProportionalElectionResults.FirstAsync(x =>
            x.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen));
        result.MatchSnapshot(x => x.CountingCircleId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .EnterCountOfVotersAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private EnterProportionalElectionCountOfVotersRequest NewValidRequest(
        Action<EnterProportionalElectionCountOfVotersRequest>? customizer = null)
    {
        var r = new EnterProportionalElectionCountOfVotersRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
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
