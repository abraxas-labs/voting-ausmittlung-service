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
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public class VoteResultCreateBundleTest : VoteResultBundleBaseTest
{
    private readonly Abraxas.Voting.Ausmittlung.Services.V1.VoteResultService.VoteResultServiceClient _voteResultClient;

    public VoteResultCreateBundleTest(TestApplicationFactory factory)
        : base(factory)
    {
        _voteResultClient = CreateService<Abraxas.Voting.Ausmittlung.Services.V1.VoteResultService.VoteResultServiceClient>(
            RolesMockedData.ErfassungElectionAdmin);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                BallotBundleSampleSizePercent = 30,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var ballotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult);
        var bundle3Id = Guid.Parse("13351714-0973-41be-992d-d2b32605db5d");
        var bundle4Id = Guid.Parse("cdaea8af-9455-46cb-8437-1c29eea8db43");

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new VoteResultBundleCreated
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
                BundleId = bundle3Id.ToString(),
                BundleNumber = 4,
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
            new VoteResultBundleCreated
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
                BundleId = bundle4Id.ToString(),
                BundleNumber = 5,
                EventInfo = new EventInfo
                {
                    Timestamp = new Timestamp
                    {
                        Seconds = 1594980500,
                    },
                    Tenant = SecureConnectTestDefaults.MockedTenantGossau.ToEventInfoTenant(),
                    User = new() { Id = TestDefaults.UserId },
                },
            });

        var bundles = await ErfassungElectionAdminClient.GetBundlesAsync(
            new GetVoteResultBundlesRequest
            {
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            });
        bundles.MatchSnapshot();

        var result = await GetBallotResult();
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(5);

        await AssertHasPublishedEventProcessedMessage(VoteResultBundleCreated.Descriptor, bundle3Id);
    }

    [Fact]
    public async Task TestShouldBeOk()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdStGallenEvoting, async () =>
        {
            await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleNumberEntered>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<VoteResultBundleCreated>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldNotReuseDeletedBundleNumberIfAuto()
    {
        var bundleResp = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        await RunEvents<VoteResultBundleCreated>();
        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteVoteResultBundleRequest
            {
                BundleId = bundleResp.BundleId,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            });
        var bundleResp2 = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        bundleResp.BundleNumber.Should().Be(bundleResp2.BundleNumber - 1);
    }

    [Fact]
    public async Task TestShouldBeOkAsCreator()
    {
        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldThrowIfFinalResultsEntry()
    {
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.FinalResults,
        });
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "bundle number is not generated automatically and should be provided");
    }

    [Fact]
    public async Task TestManualBundleNumberShouldBeOkButThrowForZero()
    {
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                BallotBundleSampleSizePercent = 100,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        });

        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument);

        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 0)),
            StatusCode.InvalidArgument);

        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberEntered>().MatchSnapshot("numberEntered");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleCreated>().MatchSnapshot("created", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientStGallen.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
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
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                BallotBundleSampleSizePercent = 100,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        });

        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10)),
            StatusCode.InvalidArgument,
            "bundle number is already in use");
    }

    [Fact]
    public async Task TestShouldThrowDuplicatedManualBundleNumberAfterDelete()
    {
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                BallotBundleSampleSizePercent = 100,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        });

        var bundleResponse = await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        await RunEvents<VoteResultBundleCreated>();
        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteVoteResultBundleRequest
            {
                BundleId = bundleResponse.BundleId,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
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
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                BallotBundleSampleSizePercent = 100,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        });

        var bundleResp = await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        await RunEvents<VoteResultBundleCreated>();

        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteVoteResultBundleRequest
            {
                BundleId = bundleResp.BundleId,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            });
        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
    }

    [Fact]
    public async Task TestShouldRestartBundleNumberAfterDefineEntry()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        await _voteResultClient.DefineEntryAsync(new DefineVoteResultEntryRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            ResultEntry = SharedProto.VoteResultEntry.Detailed,
            ResultEntryParams = new DefineVoteResultEntryParamsRequest
            {
                BallotBundleSampleSizePercent = 100,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.VoteReviewProcedure.Electronically,
            },
        });

        EventPublisherMock.Clear();
        var response = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        response.BundleNumber.Should().Be(1);

        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleNumberEntered>()
            .BundleNumber
            .Should()
            .Be(1);

        EventPublisherMock.GetSinglePublishedEvent<VoteResultBundleCreated>()
            .BundleNumber
            .Should()
            .Be(1);
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
            db.VoteResultBundles.RemoveRange(db.VoteResultBundles);
            await db.SaveChangesAsync();
        });
        await RunToState(state);
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "This operation is not possible for state");
    }

    [Fact]
    public async Task TestShouldThrowContestLocked()
    {
        await SetContestState(ContestMockedData.IdStGallenEvoting, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new VoteResultBundleService.VoteResultBundleServiceClient(channel)
            .CreateBundleAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private CreateVoteResultBundleRequest NewValidRequest(
        Action<CreateVoteResultBundleRequest>? customizer = null)
    {
        var r = new CreateVoteResultBundleRequest
        {
            VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
        };
        customizer?.Invoke(r);
        return r;
    }
}
