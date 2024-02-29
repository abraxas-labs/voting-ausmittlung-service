// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public class ProportionalElectionResultGetTest : BaseTest<ProportionalElectionResultService.ProportionalElectionResultServiceClient>
{
    private const string IdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";

    public ProportionalElectionResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ContestMockedData.Seed(RunScoped);
        await ProportionalElectionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithResults()
    {
        var resultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen;
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionUnmodifiedListResultsEntered
        {
            ElectionResultId = resultId,
            Results =
                {
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        VoteCount = 100,
                        ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                    },
                },
            EventInfo = GetMockedEventInfo(),
        });

        var bundleId = "82be0025-3b83-4f41-8b12-ad46d755067b";
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBallotCreated
        {
            BallotNumber = 1,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBallotCreated
        {
            BallotNumber = 2,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });

        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.AllBundlesReviewedOrDeleted.Should().BeFalse();
        response.TotalCountOfUnmodifiedLists.Should().Be(100);
        response.TotalCountOfBallots.Should().Be(0);
        response.TotalCountOfLists.Should().Be(100);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleSubmissionFinished
            {
                BundleId = bundleId,
                ElectionResultId = resultId,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewSucceeded
            {
                BundleId = bundleId,
                EventInfo = GetMockedEventInfo(),
            });

        response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.AllBundlesReviewedOrDeleted.Should().BeTrue();
        response.TotalCountOfUnmodifiedLists.Should().Be(100);
        response.TotalCountOfBallots.Should().Be(2);
        response.TotalCountOfLists.Should().Be(102);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithDeletedAndReviewedBundles()
    {
        var resultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen;
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionUnmodifiedListResultsEntered
        {
            ElectionResultId = resultId,
            Results =
                {
                    new ProportionalElectionUnmodifiedListResultEventData
                    {
                        VoteCount = 100,
                        ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
                    },
                },
            EventInfo = GetMockedEventInfo(),
        });

        var bundleId = "82be0025-3b83-4f41-8b12-ad46d755067b";
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBallotCreated
        {
            BallotNumber = 1,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleSubmissionFinished
            {
                BundleId = bundleId,
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewSucceeded
            {
                BundleId = bundleId,
                EventInfo = GetMockedEventInfo(),
            });

        var bundleId2 = "98a8b920-6f84-4aef-b467-e4f3718a33f5";
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId2,
            BundleNumber = 2,
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBallotCreated
        {
            BallotNumber = 2,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleSubmissionFinished
            {
                BundleId = bundleId2,
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleReviewSucceeded
            {
                BundleId = bundleId2,
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleDeleted
            {
                BundleId = bundleId2,
                EventInfo = GetMockedEventInfo(),
            });

        var bundleId3 = "9dbbc6c7-2f56-4a22-a3ae-caabe7b2e44e";
        await TestEventPublisher.Publish(GetNextEventNumber(), new ProportionalElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId3,
            BundleNumber = 2,
            ResultEntryParams = new ProportionalElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleDeleted
            {
                BundleId = bundleId3,
                EventInfo = GetMockedEventInfo(),
            });
        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.AllBundlesReviewedOrDeleted.Should().BeTrue();
        response.TotalCountOfBallots.Should().Be(1);
        response.TotalCountOfUnmodifiedLists.Should().Be(100);
        response.TotalCountOfLists.Should().Be(101);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithResultId()
    {
        var response = await ErfassungCreatorClient.GetAsync(new GetProportionalElectionResultRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdmin()
    {
        var response = await ErfassungElectionAdminClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungElectionAdminWithResultId()
    {
        var response = await ErfassungElectionAdminClient.GetAsync(new GetProportionalElectionResultRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdmin()
    {
        var response = await MonitoringElectionAdminClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsMonitoringElectionAdminWithResultId()
    {
        var response = await MonitoringElectionAdminClient.GetAsync(new GetProportionalElectionResultRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldThrowNotFound()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(NewValidRequest(r => r.ElectionId = IdNotFound)),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowNotFoundResultId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetProportionalElectionResultRequest
            {
                ElectionResultId = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetProportionalElectionResultRequest
            {
                ElectionId = ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds,
                CountingCircleId = CountingCircleMockedData.IdUzwil,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenantResultId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetProportionalElectionResultRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdUzwilElectionResultInContestUzwil,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultService.ProportionalElectionResultServiceClient(channel)
            .GetAsync(NewValidRequest());
    }

    protected override IEnumerable<string> UnauthorizedRoles()
    {
        yield return NoRole;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    private GetProportionalElectionResultRequest NewValidRequest(Action<GetProportionalElectionResultRequest>? customizer = null)
    {
        var r = new GetProportionalElectionResultRequest
        {
            ElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        };
        customizer?.Invoke(r);
        return r;
    }
}
