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
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public class MajorityElectionResultGetTest : MajorityElectionResultBaseTest
{
    private const string IdNotFound = "a5be0aba-9e39-407c-ac61-ffd2fa08f410";

    public MajorityElectionResultGetTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);

        await ErfassungElectionAdminClient.DefineEntryAsync(new DefineMajorityElectionResultEntryRequest
        {
            ResultEntry = SharedProto.MajorityElectionResultEntry.FinalResults,
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
        });
        await RunEvents<MajorityElectionResultEntryDefined>();

        await ErfassungElectionAdminClient.EnterCandidateResultsAsync(
            new EnterMajorityElectionCandidateResultsRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                IndividualVoteCount = 1,
                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                {
                    ConventionalReceivedBallots = 1,
                    ConventionalAccountedBallots = 100,
                },
                CandidateResults =
                {
                        new EnterMajorityElectionCandidateResultRequest
                        {
                            VoteCount = 10,
                            CandidateId = MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund,
                        },
                        new EnterMajorityElectionCandidateResultRequest
                        {
                            VoteCount = 15,
                            CandidateId = MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                        },
                },
                SecondaryElectionCandidateResults =
                {
                        new EnterSecondaryMajorityElectionCandidateResultsRequest
                        {
                            SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                            CandidateResults =
                            {
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 11,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                                },
                                new EnterMajorityElectionCandidateResultRequest
                                {
                                    VoteCount = 7,
                                    CandidateId = MajorityElectionMockedData.SecondaryElectionCandidateId2StGallenMajorityElectionInContestBund,
                                },
                            },
                        },
                },
            });
        await RunEvents<MajorityElectionCandidateResultsEntered>();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreator()
    {
        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorOrderedByCount()
    {
        await RunOnDb(async db =>
        {
            var result = await db.MajorityElectionResults
                .AsTracking()
                .FirstAsync(x =>
                    x.Id == MajorityElectionResultMockedData.GuidStGallenElectionResultInContestBund);
            result.Entry = MajorityElectionResultEntry.Detailed;
            await db.SaveChangesAsync();
        });
        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.MatchSnapshot();
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithoutSecondaryElections()
    {
        var response = await ErfassungCreatorClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallenWithoutChilds,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        });
        response.MatchSnapshot(x => x.Id);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithResults()
    {
        var resultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund;
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionBallotGroupResultsEntered
        {
            ElectionResultId = resultId,
            Results =
                {
                    new MajorityElectionBallotGroupResultEventData
                    {
                        VoteCount = 100,
                        BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                    },
                },
            EventInfo = GetMockedEventInfo(),
        });

        var bundleId = "82be0025-3b83-4f41-8b12-ad46d755067b";
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new MajorityElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBallotCreated
        {
            BallotNumber = 1,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBallotCreated
        {
            BallotNumber = 2,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });

        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.AllBundlesReviewedOrDeleted.Should().BeFalse();
        response.ConventionalCountOfBallotGroupVotes.Should().Be(100);
        response.ConventionalCountOfDetailedEnteredBallots.Should().Be(0);

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleSubmissionFinished
            {
                BundleId = bundleId,
                ElectionResultId = resultId,
                EventInfo = GetMockedEventInfo(),
            });

        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewSucceeded
            {
                BundleId = bundleId,
                EventInfo = GetMockedEventInfo(),
            });

        response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.AllBundlesReviewedOrDeleted.Should().BeTrue();
        response.ConventionalCountOfBallotGroupVotes.Should().Be(100);
        response.ConventionalCountOfDetailedEnteredBallots.Should().Be(2);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithDeletedAndReviewedBundles()
    {
        var resultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund;
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionBallotGroupResultsEntered
        {
            ElectionResultId = resultId,
            Results =
                {
                    new MajorityElectionBallotGroupResultEventData
                    {
                        VoteCount = 100,
                        BallotGroupId = MajorityElectionMockedData.BallotGroupIdStGallenMajorityElectionInContestBund,
                    },
                },
            EventInfo = GetMockedEventInfo(),
        });

        var bundleId = "82be0025-3b83-4f41-8b12-ad46d755067b";
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId,
            BundleNumber = 1,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new MajorityElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBallotCreated
        {
            BallotNumber = 1,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleSubmissionFinished
            {
                BundleId = bundleId,
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewSucceeded
            {
                BundleId = bundleId,
                EventInfo = GetMockedEventInfo(),
            });

        var bundleId2 = "98a8b920-6f84-4aef-b467-e4f3718a33f5";
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId2,
            BundleNumber = 2,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new MajorityElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBallotCreated
        {
            BallotNumber = 2,
            BundleId = bundleId,
            ElectionResultId = resultId,
            EmptyVoteCount = 3,
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleSubmissionFinished
            {
                BundleId = bundleId2,
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleReviewSucceeded
            {
                BundleId = bundleId2,
                EventInfo = GetMockedEventInfo(),
            });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleDeleted
            {
                BundleId = bundleId2,
                EventInfo = GetMockedEventInfo(),
            });

        var bundleId3 = "9dbbc6c7-2f56-4a22-a3ae-caabe7b2e44e";
        await TestEventPublisher.Publish(GetNextEventNumber(), new MajorityElectionResultBundleCreated
        {
            ElectionResultId = resultId,
            BundleId = bundleId3,
            BundleNumber = 2,
            ResultEntry = SharedProto.MajorityElectionResultEntry.Detailed,
            ResultEntryParams = new MajorityElectionResultEntryParamsEventData(),
            EventInfo = GetMockedEventInfo(),
        });
        await TestEventPublisher.Publish(
            GetNextEventNumber(),
            new MajorityElectionResultBundleDeleted
            {
                BundleId = bundleId3,
                EventInfo = GetMockedEventInfo(),
            });
        var response = await ErfassungCreatorClient.GetAsync(NewValidRequest());
        response.AllBundlesReviewedOrDeleted.Should().BeTrue();
        response.ConventionalCountOfDetailedEnteredBallots.Should().Be(1);
        response.ConventionalCountOfBallotGroupVotes.Should().Be(100);
    }

    [Fact]
    public async Task TestShouldReturnAsErfassungCreatorWithResultId()
    {
        var response = await ErfassungCreatorClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
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
        var response = await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
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
        var response = await MonitoringElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
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
            async () => await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
            {
                ElectionResultId = IdNotFound,
            }),
            StatusCode.NotFound);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenant()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
            {
                ElectionId = MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche,
                CountingCircleId = CountingCircleMockedData.IdUzwilKirche,
            }),
            StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task TestShouldThrowOtherTenantResultId()
    {
        await AssertStatus(
            async () => await ErfassungElectionAdminClient.GetAsync(new GetMajorityElectionResultRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdKircheElectionResultInContestKirche,
            }),
            StatusCode.PermissionDenied);
    }

    protected override async Task AuthorizationTestCall(GrpcChannel channel)
    {
        await new MajorityElectionResultService.MajorityElectionResultServiceClient(channel)
            .GetAsync(NewValidRequest());
    }

    protected override IEnumerable<string> AuthorizedRoles()
    {
        yield return RolesMockedData.ErfassungCreator;
        yield return RolesMockedData.ErfassungCreatorWithoutBundleControl;
        yield return RolesMockedData.ErfassungBundleController;
        yield return RolesMockedData.ErfassungElectionSupporter;
        yield return RolesMockedData.ErfassungElectionAdmin;
        yield return RolesMockedData.MonitoringElectionAdmin;
        yield return RolesMockedData.MonitoringElectionSupporter;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    private GetMajorityElectionResultRequest NewValidRequest(Action<GetMajorityElectionResultRequest>? customizer = null)
    {
        var r = new GetMajorityElectionResultRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        };
        customizer?.Invoke(r);
        return r;
    }
}
