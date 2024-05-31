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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public class ProportionalElectionResultCreateBundleTest : ProportionalElectionResultBundleBaseTest
{
    private ProportionalElectionResultService.ProportionalElectionResultServiceClient _resultClient = null!;

    public ProportionalElectionResultCreateBundleTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        _resultClient = CreateService<ProportionalElectionResultService.ProportionalElectionResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                BallotBundleSampleSize = 3,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = true,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        var bundle1Id = Guid.Parse("0e6ce300-8106-49e2-824d-1311de811994");
        var bundle2Id = Guid.Parse("b1e326d7-2f47-43d7-8430-cf150401337c");
        var bundle3Id = Guid.Parse("e9590e17-abae-4e41-a575-08d1f5d79db3");
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new ProportionalElectionResultBundleCreated
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                BundleId = bundle1Id.ToString(),
                BundleNumber = 1,
                EventInfo = new EventInfo
                {
                    Timestamp = new Timestamp
                    {
                        Seconds = 1594980476,
                    },
                    Tenant = SecureConnectTestDefaults.MockedTenantGossau.ToEventInfoTenant(),
                    User = new() { Id = TestDefaults.UserId },
                },
            },
            new ProportionalElectionResultBundleCreated
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                BundleId = bundle2Id.ToString(),
                BundleNumber = 2,
                EventInfo = new EventInfo
                {
                    Timestamp = new Timestamp
                    {
                        Seconds = 1594980500,
                    },
                    Tenant = SecureConnectTestDefaults.MockedTenantGossau.ToEventInfoTenant(),
                    User = new() { Id = TestDefaults.UserId },
                },
            },
            new ProportionalElectionResultBundleCreated
            {
                ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
                BundleId = bundle3Id.ToString(),
                BundleNumber = 3,
                EventInfo = new EventInfo
                {
                    Timestamp = new Timestamp
                    {
                        Seconds = 1594980600,
                    },
                    Tenant = SecureConnectTestDefaults.MockedTenantGossau.ToEventInfoTenant(),
                    User = SecureConnectTestDefaults.MockedUserService.ToEventInfoUser(),
                },
            });

        var bundles = await ErfassungElectionAdminClient.GetBundlesAsync(
            new GetProportionalElectionResultBundlesRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
            });
        bundles.MatchSnapshot();

        var result = await GetElectionResult();
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(6);
        result.TotalCountOfBallots.Should().Be(0);
        result.TotalCountOfLists.Should().Be(0);
        result.TotalCountOfUnmodifiedLists.Should().Be(0);
        result.TotalCountOfListsWithoutParty.Should().Be(0);

        await AssertHasPublishedMessage<ProportionalElectionBundleChanged>(
            x => x.Id == bundle1Id && x.ElectionResultId == resultId);

        await AssertHasPublishedMessage<ProportionalElectionBundleChanged>(
            x => x.Id == bundle2Id && x.ElectionResultId == resultId);
    }

    [Fact]
    public async Task TestShouldBeOk()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldRestartBundleNumberAfterDefineEntry()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());

        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 1,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 1,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
        });

        EventPublisherMock.Clear();
        var response = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        response.BundleNumber.Should().Be(1);

        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberEntered>()
            .BundleNumber
            .Should()
            .Be(1);

        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCreated>()
            .BundleNumber
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task TestShouldNotReuseDeletedBundleNumberIfAuto()
    {
        var bundleResp = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        await RunEvents<ProportionalElectionResultBundleCreated>();
        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = bundleResp.BundleId,
            });
        var bundleResp2 = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        bundleResp.BundleNumber.Should().Be(bundleResp2.BundleNumber - 1);
    }

    [Fact]
    public async Task TestWithoutListShouldBeOk()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest(x => x.ListId = string.Empty));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldBeOkAsCreator()
    {
        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest());
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleNumberEntered>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<ProportionalElectionResultBundleCreated>(),
            };
        });
    }

    [Fact]
    public async Task TestManualBundleNumberShouldBeOkButThrowForZero()
    {
        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 10,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        });

        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument);

        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 0)),
            StatusCode.InvalidArgument);

        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberEntered>().MatchSnapshot("numberEntered");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCreated>().MatchSnapshot("created", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientStGallen.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<ProportionalElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientStGallen.CreateBundleAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedManualBundleNumber()
    {
        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 10,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        });

        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10)),
            StatusCode.InvalidArgument,
            "bundle number is already in use");
    }

    [Fact]
    public async Task TestShouldReturnIfDeletedBundleNumberIsReused()
    {
        await _resultClient.DefineEntryAsync(new DefineProportionalElectionResultEntryRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ResultEntryParams = new DefineProportionalElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 10,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.ProportionalElectionReviewProcedure.Electronically,
            },
        });

        var bundleResp = await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        await RunEvents<ProportionalElectionResultBundleCreated>();
        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = bundleResp.BundleId,
            });
        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
    }

    [Theory]
    [InlineData(CountingCircleResultState.SubmissionDone)]
    [InlineData(CountingCircleResultState.CorrectionDone)]
    [InlineData(CountingCircleResultState.AuditedTentatively)]
    [InlineData(CountingCircleResultState.Plausibilised)]
    public async Task TestShouldThrowInWrongState(CountingCircleResultState state)
    {
        // remove all existing bundles to be able to easily switch to other states which require all bundles to be reviewed
        await RunOnDb(async db =>
        {
            db.ProportionalElectionBundles.RemoveRange(db.ProportionalElectionBundles);
            await db.SaveChangesAsync();
        });
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowLockedContest()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(channel)
            .CreateBundleAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private CreateProportionalElectionResultBundleRequest NewValidRequest(
        Action<CreateProportionalElectionResultBundleRequest>? customizer = null)
    {
        var r = new CreateProportionalElectionResultBundleRequest
        {
            ElectionResultId = ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen,
            ListId = ProportionalElectionMockedData.ListId1GossauProportionalElectionInContestStGallen,
        };
        customizer?.Invoke(r);
        return r;
    }
}
