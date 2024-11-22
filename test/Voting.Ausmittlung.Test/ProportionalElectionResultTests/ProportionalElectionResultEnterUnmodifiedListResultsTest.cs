// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultEnterUnmodifiedListResultsTest : ProportionalElectionResultBaseTest
{
    private const string IdNotFound = "8b89b1a7-90a8-4b38-9422-812545bbadbb";
    private const string IdBadFormat = "8b89b1a790a8-4b38-9422-812545bbadbb";

    public ProportionalElectionResultEnterUnmodifiedListResultsTest(TestApplicationFactory factory)
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
        await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnmodifiedListResultsEntered>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(NewValidRequest());
            return EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionUnmodifiedListResultsEntered>();
        });
    }

    [Fact]
    public async Task TestShouldReturnAsContestManagerDuringTestingPhase()
    {
        await StGallenErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionUnmodifiedListResultsEntered>().MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await StGallenErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(
                NewValidRequest(r => r.ElectionResultId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    [Fact]
    public async Task TestShouldThrowBadId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(
                NewValidRequest(r => r.ElectionResultId = IdBadFormat)),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(
                NewValidRequest(r => r.ElectionResultId = ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil)),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowAlreadySubmitted()
    {
        await RunToState(CountingCircleResultState.SubmissionDone);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(NewValidRequest()),
            StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task TestShouldThrowUnrelatedListResults()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.EnterUnmodifiedListResultsAsync(
                NewValidRequest(r =>
                    r.Results[0].ListId = ProportionalElectionMockedData.ListIdBundProportionalElectionInContestBund)),
            StatusCode.InvalidArgument,
            "lists provided which don't exist");
    }

    [Fact]
    public async Task TestProcessor()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnmodifiedListResultsEntered
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                Results =
                {
                        new ProportionalElectionUnmodifiedListResultEventData
                        {
                            ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                            VoteCount = 100,
                        },
                        new ProportionalElectionUnmodifiedListResultEventData
                        {
                            ListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                            VoteCount = 500,
                        },
                        new ProportionalElectionUnmodifiedListResultEventData
                        {
                            ListId = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                            VoteCount = 10,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });
        var results = await ErfassungElectionAdminClient.GetUnmodifiedListsAsync(
            new GetProportionalElectionUnmodifiedListResultsRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
            });
        results.MatchSnapshot();

        var result = await RunOnDb(db => db.ProportionalElectionResults
            .FirstAsync(x => x.Id == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen));
        result.TotalCountOfUnmodifiedLists.Should().Be(610);
    }

    [Fact]
    public async Task TestProcessorUpdatesListResults()
    {
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionUnmodifiedListResultsEntered
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                Results =
                {
                        new ProportionalElectionUnmodifiedListResultEventData
                        {
                            ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                            VoteCount = 100,
                        },
                        new ProportionalElectionUnmodifiedListResultEventData
                        {
                            ListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                            VoteCount = 500,
                        },
                        new ProportionalElectionUnmodifiedListResultEventData
                        {
                            ListId = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                            VoteCount = 10,
                        },
                },
                EventInfo = GetMockedEventInfo(),
            });

        var listResults = await ErfassungElectionAdminClient.GetListResultsAsync(
            new GetProportionalElectionListResultsRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
            });

        foreach (var listResult in listResults.ListResults)
        {
            listResult.Id = string.Empty;
            foreach (var candidateResult in listResult.CandidateResults)
            {
                candidateResult.Id = string.Empty;
            }
        }

        listResults.MatchSnapshot();
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .EnterUnmodifiedListResultsAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private EnterProportionalElectionUnmodifiedListResultsRequest NewValidRequest(Action<EnterProportionalElectionUnmodifiedListResultsRequest>? customizer = null)
    {
        var r = new EnterProportionalElectionUnmodifiedListResultsRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            Results =
                {
                    new EnterProportionalElectionUnmodifiedListResultRequest
                    {
                        ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                        VoteCount = 100,
                    },
                    new EnterProportionalElectionUnmodifiedListResultRequest
                    {
                        ListId = ProportionalElectionMockedData.ListId2GossauProportionalElectionInContestStGallen,
                        VoteCount = 500,
                    },
                    new EnterProportionalElectionUnmodifiedListResultRequest
                    {
                        ListId = ProportionalElectionMockedData.ListId3GossauProportionalElectionInContestStGallen,
                        VoteCount = 10,
                    },
                },
        };
        customizer?.Invoke(r);
        return r;
    }
}
