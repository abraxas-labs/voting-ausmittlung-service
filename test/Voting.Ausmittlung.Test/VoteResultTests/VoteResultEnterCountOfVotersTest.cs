// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Snapper;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public class VoteResultEnterCountOfVotersTest : VoteResultBaseTest
{
    private const string IdNotFound = "ba2060f1-fc0c-4752-ae28-8dcda3837f41";
    private const string IdBadFormat = "ba2060f1fc0c-4752-ae28-8dcda3837f41";

    public VoteResultEnterCountOfVotersTest(TestApplicationFactory factory)
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
        EventPublisherMock
            .GetSinglePublishedEvent<VoteResultCountOfVotersEntered>()
            .ShouldMatchChildSnapshot("event");

        await RunEvents<VoteResultCountOfVotersEntered>(false);

        await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());

        var result = await ErfassungElectionAdminClient.GetAsync(new GetVoteResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            CountingCircleId = CountingCircleMockedData.IdGossau,
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
        });
        result.ShouldMatchChildSnapshot("data");
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.EnterCountOfVotersAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultCountOfVotersEntered>();
        });
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
                NewValidRequest(r => r.VoteResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r => r.VoteResultId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
                NewValidRequest(r =>
                    r.VoteResultId = VoteResultMockedData.IdUzwilVoteInContestStGallenResult)),
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
            new VoteResultCountOfVotersEntered
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                ResultsCountOfVoters =
                {
                        new VoteBallotResultsCountOfVotersEventData
                        {
                            BallotId = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                            CountOfVoters = new PoliticalBusinessCountOfVotersEventData
                            {
                                ConventionalReceivedBallots = 10000,
                                ConventionalAccountedBallots = 2000,
                                ConventionalBlankBallots = 1000,
                                ConventionalInvalidBallots = 500,
                            },
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var result = await RunOnDb(db => db.VoteResults
            .Include(x => x.Results)
            .FirstAsync(x => x.Id == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult)));
        result.Results = result.Results.OrderBy(x => x.CountOfVoters.VoterParticipation).ToList();
        result.MatchSnapshot(x => x.CountingCircleId);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultService.VoteResultServiceClient(channel)
            .EnterCountOfVotersAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.MonitoringElectionAdmin;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private EnterVoteResultCountOfVotersRequest NewValidRequest(
        Action<EnterVoteResultCountOfVotersRequest>? customizer = null)
    {
        var r = new EnterVoteResultCountOfVotersRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultsCountOfVoters =
                {
                    new EnterVoteBallotResultsCountOfVotersRequest
                    {
                        BallotId = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                        CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                        {
                            ConventionalReceivedBallots = 14000,
                            ConventionalAccountedBallots = 12000,
                            ConventionalBlankBallots = 1000,
                            ConventionalInvalidBallots = 500,
                        },
                    },
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
