// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MajorityElectionResultTests;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public abstract class MajorityElectionResultBundleBaseTest : MajorityElectionResultBaseTest
{
    protected MajorityElectionResultBundleBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleMonitoringElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleErfassungElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleErfassungCreatorClient { get; private set; } = null!; // initialized during InitializeAsync

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleErfassungElectionAdminClientSecondUser { get; private set; } = null!; // initialized during InitializeAsync

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleErfassungCreatorClientSecondUser { get; private set; } = null!; // initialized during InitializeAsync

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleErfassungElectionAdminClientBund { get; private set; } = null!; // initialized during InitializeAsync

    protected int LatestBallotNumber { get; private set; }

    public override async Task InitializeAsync()
    {
        BundleMonitoringElectionAdminClient = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.MonitoringElectionAdmin));
        BundleErfassungElectionAdminClient = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin));
        BundleErfassungCreatorClient = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungCreator));
        BundleErfassungElectionAdminClientSecondUser = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        BundleErfassungCreatorClientSecondUser = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, "my-user-99", RolesMockedData.ErfassungCreator));
        BundleErfassungElectionAdminClientBund = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
    }

    protected async Task RunBundleToState(BallotBundleState state)
    {
        switch (state)
        {
            case BallotBundleState.InCorrection:
                await RunBundleToState(BallotBundleState.ReadyForReview);
                await SetBundleInCorrection();
                break;
            case BallotBundleState.ReadyForReview:
                await CreateBallot();
                await SetBundleSubmissionFinished();
                break;
            case BallotBundleState.Reviewed:
                await RunBundleToState(BallotBundleState.ReadyForReview);
                await SetBundleReviewed();
                break;
            case BallotBundleState.Deleted:
                await SetBundleDeleted();
                break;
        }
    }

    protected async Task SetBundleSubmissionFinished()
    {
        await BundleErfassungCreatorClient
            .BundleSubmissionFinishedAsync(new MajorityElectionResultBundleSubmissionFinishedRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        await RunEvents<MajorityElectionResultBundleSubmissionFinished>();
    }

    protected async Task SetBundleInCorrection()
    {
        await BundleErfassungCreatorClientSecondUser
            .RejectBundleReviewAsync(new RejectMajorityElectionBundleReviewRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        await RunEvents<MajorityElectionResultBundleReviewRejected>();
    }

    protected async Task SetBundleReviewed()
    {
        await BundleErfassungCreatorClientSecondUser
            .SucceedBundleReviewAsync(new SucceedMajorityElectionBundleReviewRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        await RunEvents<MajorityElectionResultBundleReviewSucceeded>();
    }

    protected async Task SetBundleDeleted()
    {
        await BundleErfassungElectionAdminClient
            .DeleteBundleAsync(new DeleteMajorityElectionResultBundleRequest
            {
                BundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1,
            });
        await RunEvents<MajorityElectionResultBundleDeleted>();
    }

    protected async Task<CreateMajorityElectionResultBallotResponse> CreateBallot(string bundleId = MajorityElectionResultBundleMockedData.IdStGallenBundle1)
    {
        var ballotResponse = await BundleErfassungCreatorClient.CreateBallotAsync(new CreateMajorityElectionResultBallotRequest
        {
            BundleId = bundleId,
            SelectedCandidateIds =
                {
                    MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund,
                },
            SecondaryMajorityElectionResults =
                {
                    new CreateUpdateSecondaryMajorityElectionResultBallotRequest
                    {
                        EmptyVoteCount = 1,
                        IndividualVoteCount = 1,
                        SecondaryMajorityElectionId = MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund,
                        SelectedCandidateIds =
                        {
                            MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund,
                        },
                    },
                },
            IndividualVoteCount = 0,
            EmptyVoteCount = 0,
        });
        LatestBallotNumber = ballotResponse.BallotNumber;
        await RunEvents<MajorityElectionResultBallotCreated>();
        return ballotResponse;
    }

    protected Task<MajorityElectionResultBundle> GetBundle(Guid? id = null)
    {
        id ??= Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1);
        return RunOnDb(db =>
            db.MajorityElectionResultBundles
                .Include(x => x.ElectionResult)
                .Where(x => x.Id == id)
                .FirstAsync());
    }

    protected Task<MajorityElectionResult> GetElectionResult(Guid? id = null)
    {
        id ??= Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund);
        return RunOnDb(db =>
            db.MajorityElectionResults
                .Where(x => x.Id == id)
                .FirstAsync());
    }

    protected async Task ShouldHaveCandidateResults(bool haveResults)
    {
        var hasNotZeroCandidateResults = await RunOnDb(db => db.MajorityElectionCandidateResults
            .AnyAsync(c =>
                c.ElectionResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund) &&
                c.VoteCount != 0));
        hasNotZeroCandidateResults.Should().Be(haveResults);

        var hasNotZeroSecondaryCandidateResults = await RunOnDb(db => db.SecondaryMajorityElectionCandidateResults
            .AnyAsync(c =>
                c.ElectionResult.PrimaryResultId == Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund) &&
                c.VoteCount != 0));
        hasNotZeroSecondaryCandidateResults.Should().Be(haveResults);
    }
}
