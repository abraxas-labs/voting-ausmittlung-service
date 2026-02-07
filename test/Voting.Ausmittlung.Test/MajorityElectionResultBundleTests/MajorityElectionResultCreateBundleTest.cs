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

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public class MajorityElectionResultCreateBundleTest : MajorityElectionResultBundleBaseTest
{
    private readonly MajorityElectionResultService.MajorityElectionResultServiceClient _resultClient;

    public MajorityElectionResultCreateBundleTest(TestApplicationFactory factory)
        : base(factory)
    {
        _resultClient = CreateService<MajorityElectionResultService.MajorityElectionResultServiceClient>(RolesMockedData.ErfassungElectionAdmin);
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await _resultClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                BallotBundleSampleSize = 3,
                AutomaticBallotBundleNumberGeneration = true,
                AutomaticEmptyVoteCounting = true,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });
    }

    [Fact]
    public async Task TestProcessor()
    {
        var resultId = MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund;
        var bundle3Id = Guid.Parse("0e6ce300-8106-49e2-824d-1311de811994");
        var bundle4Id = Guid.Parse("b1e326d7-2f47-43d7-8430-cf150401337c");

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleCreated
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
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
            new MajorityElectionResultBundleCreated
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
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
            new GetMajorityElectionResultBundlesRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            });
        bundles.MatchSnapshot();

        var result = await GetElectionResult();
        result.AllBundlesReviewedOrDeleted.Should().BeFalse();
        result.CountOfBundlesNotReviewedOrDeleted.Should().Be(5);

        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultBundleCreated.Descriptor, bundle3Id);
        await AssertHasPublishedEventProcessedMessage(MajorityElectionResultBundleCreated.Descriptor, bundle4Id);
    }

    [Fact]
    public async Task TestShouldBeOk()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldNotReuseDeletedBundleNumberIfAuto()
    {
        var bundleResp = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        await RunEvents<MajorityElectionResultBundleCreated>();
        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = bundleResp.BundleId,
            });
        var bundleResp2 = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        bundleResp.BundleNumber.Should().Be(bundleResp2.BundleNumber - 1);
    }

    [Fact]
    public async Task TestShouldBeOkAsCreator()
    {
        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldCreateSignature()
    {
        await TestEventsWithSignature(ContestMockedData.IdBundesurnengang, async () =>
        {
            await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest());
            return new[]
            {
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleNumberEntered>(),
                    EventPublisherMock.GetSinglePublishedEventWithMetadata<MajorityElectionResultBundleCreated>(),
            };
        });
    }

    [Fact]
    public async Task TestShouldBeOkAsContestManagerDuringTestingPhase()
    {
        await BundleErfassungElectionAdminClientBund.CreateBundleAsync(NewValidRequest());
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberEntered>().MatchSnapshot("1");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCreated>().MatchSnapshot("2", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldThrowAsContestManagerAfterTestingPhaseEnded()
    {
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.Active);
        await AssertStatus(
            async () => await BundleErfassungElectionAdminClientBund.CreateBundleAsync(NewValidRequest()),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowIfFinalResultsEntry()
    {
        await _resultClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
        });
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument,
            "bundle number is not generated automatically and should be provided");
    }

    [Fact]
    public async Task TestManualBundleNumberShouldBeOkButThrowForZero()
    {
        await _resultClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 10,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });

        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.InvalidArgument);

        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 0)),
            StatusCode.InvalidArgument);

        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberEntered>().MatchSnapshot("numberEntered");
        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCreated>().MatchSnapshot("created", x => x.BundleId);
    }

    [Fact]
    public async Task TestShouldCreateDuplicatedManualBundleNumber()
    {
        await _resultClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 10,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });

        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        var created = await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        created.BundleNumber.Should().Be(10);
        created.BundleId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldCreateDuplicatedManualBundleNumberAfterDelete()
    {
        await _resultClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 10,
                AutomaticBallotBundleNumberGeneration = false,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
        });

        var bundleResponse = await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        await RunEvents<MajorityElectionResultBundleCreated>();
        await ErfassungElectionAdminClient.DeleteBundleAsync(
            new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = bundleResponse.BundleId,
            });
        await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        var created = await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest(x => x.BundleNumber = 10));
        created.BundleNumber.Should().Be(10);
        created.BundleId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestShouldRestartBundleNumberAfterDefineEntry()
    {
        await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        await _resultClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new DefineMajorityElectionResultEntryParamsRequest
            {
                BallotBundleSize = 1,
                BallotNumberGeneration = SharedProto.BallotNumberGeneration.RestartForEachBundle,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 1,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = SharedProto.MajorityElectionReviewProcedure.Electronically,
            },
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });

        EventPublisherMock.Clear();
        var response = await ErfassungElectionAdminClient.CreateBundleAsync(NewValidRequest());
        response.BundleNumber.Should().Be(1);

        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleNumberEntered>()
            .BundleNumber
            .Should()
            .Be(1);

        EventPublisherMock.GetSinglePublishedEvent<MajorityElectionResultBundleCreated>()
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
            db.MajorityElectionResultBundles.RemoveRange(db.MajorityElectionResultBundles);
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
        await SetContestState(ContestMockedData.IdBundesurnengang, ContestState.PastLocked);
        await AssertStatus(
            async () => await ErfassungCreatorClient.CreateBundleAsync(NewValidRequest()),
            StatusCode.FailedPrecondition,
            "Contest is past locked or archived");
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(channel)
            .CreateBundleAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
    }

    private CreateMajorityElectionResultBundleRequest NewValidRequest(
        Action<CreateMajorityElectionResultBundleRequest>? customizer = null)
    {
        var r = new CreateMajorityElectionResultBundleRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        };
        customizer?.Invoke(r);
        return r;
    }
}
