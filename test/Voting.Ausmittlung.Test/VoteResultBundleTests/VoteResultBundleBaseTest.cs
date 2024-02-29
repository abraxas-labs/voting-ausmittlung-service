// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.VoteResultTests;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public abstract class VoteResultBundleBaseTest : VoteResultBaseTest
{
    protected VoteResultBundleBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleMonitoringElectionAdminClient { get; private set; } =
        null!; // initialized during InitializeAsync

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleErfassungElectionAdminClient { get; private set; } =
        null!; // initialized during InitializeAsync

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleErfassungCreatorClient { get; private set; } =
        null!; // initialized during InitializeAsync

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleErfassungElectionAdminClientSecondUser { get; private set; } =
        null!; // initialized during InitializeAsync

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleErfassungCreatorClientSecondUser { get; private set; } =
        null!; // initialized during InitializeAsync

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleErfassungElectionAdminClientStGallen { get; private set; } =
        null!; // initialized during InitializeAsync

    protected int LatestBallotNumber { get; private set; }

    public override async Task InitializeAsync()
    {
        BundleMonitoringElectionAdminClient = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.MonitoringElectionAdmin));
        BundleErfassungElectionAdminClient = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin));
        BundleErfassungCreatorClient = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungCreator));
        BundleErfassungElectionAdminClientSecondUser = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        BundleErfassungCreatorClientSecondUser = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, "my-user-99", RolesMockedData.ErfassungCreator));
        BundleErfassungElectionAdminClientStGallen = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ResetQuestionResults();
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await VoteResultBundleMockedData.Seed(RunScoped);
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
            .BundleSubmissionFinishedAsync(new VoteResultBundleSubmissionFinishedRequest
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<VoteResultBundleSubmissionFinished>();
    }

    protected async Task SetBundleInCorrection()
    {
        await BundleErfassungCreatorClientSecondUser
            .RejectBundleReviewAsync(new RejectVoteBundleReviewRequest
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<VoteResultBundleReviewRejected>();
    }

    protected async Task SetBundleReviewed()
    {
        await BundleErfassungCreatorClientSecondUser
            .SucceedBundleReviewAsync(new SucceedVoteBundleReviewRequest
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<VoteResultBundleReviewSucceeded>();
    }

    protected async Task SetBundleDeleted()
    {
        await BundleErfassungElectionAdminClient
            .DeleteBundleAsync(new DeleteVoteResultBundleRequest
            {
                BundleId = VoteResultBundleMockedData.IdGossauBundle1,
                BallotResultId = VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult,
            });
        await RunEvents<VoteResultBundleDeleted>();
    }

    protected async Task<CreateVoteResultBallotResponse> CreateBallot(string bundleId = VoteResultBundleMockedData.IdGossauBundle1)
    {
        var ballotResponse = await BundleErfassungCreatorClient.CreateBallotAsync(new CreateVoteResultBallotRequest
        {
            BundleId = bundleId,
            QuestionAnswers =
                {
                    new CreateUpdateVoteResultBallotQuestionAnswerRequest
                    {
                        QuestionNumber = 1,
                        Answer = SharedProto.BallotQuestionAnswer.Yes,
                    },
                    new CreateUpdateVoteResultBallotQuestionAnswerRequest
                    {
                        QuestionNumber = 2,
                        Answer = SharedProto.BallotQuestionAnswer.No,
                    },
                },
            TieBreakQuestionAnswers =
                {
                    new CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest()
                    {
                        QuestionNumber = 1,
                        Answer = SharedProto.TieBreakQuestionAnswer.Q1,
                    },
                },
        });
        LatestBallotNumber = ballotResponse.BallotNumber;
        await RunEvents<VoteResultBallotCreated>();
        return ballotResponse;
    }

    protected Task<VoteResultBundle> GetBundle(Guid? id = null)
    {
        id ??= Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1);
        return RunOnDb(db =>
            db.VoteResultBundles
                .Include(x => x.BallotResult)
                .Where(x => x.Id == id)
                .FirstAsync());
    }

    protected Task<BallotResult> GetBallotResult(Guid? id = null)
    {
        id ??= Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult);
        return RunOnDb(db =>
            db.BallotResults
                .Where(x => x.Id == id)
                .FirstAsync());
    }

    protected async Task ShouldHaveQuestionResults(bool haveResults)
    {
        var hasNotZeroQuestionResults = await RunOnDb(db => db.BallotQuestionResults.AnyAsync(c =>
            c.BallotResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult) &&
            (c.ConventionalSubTotal.TotalCountOfAnswerYes != 0 || c.ConventionalSubTotal.TotalCountOfAnswerNo != 0 ||
             c.ConventionalSubTotal.TotalCountOfAnswerUnspecified != 0)));
        hasNotZeroQuestionResults.Should().Be(haveResults);

        var hasNotZeroTieBreakQuestionResults = await RunOnDb(db => db.TieBreakQuestionResults.AnyAsync(c =>
            c.BallotResultId == Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult) &&
            (c.ConventionalSubTotal.TotalCountOfAnswerQ1 != 0 || c.ConventionalSubTotal.TotalCountOfAnswerQ2 != 0 ||
             c.ConventionalSubTotal.TotalCountOfAnswerUnspecified != 0)));
        hasNotZeroTieBreakQuestionResults.Should().Be(haveResults);
    }

    private async Task ResetQuestionResults()
    {
        var ballotResultId = Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult);
        await RunOnDb(async db =>
        {
            var ballotResult = await db.BallotResults
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.VoteResult)
                .Include(x => x.QuestionResults)
                .Include(x => x.TieBreakQuestionResults)
                .SingleAsync(x => x.Id == ballotResultId);

            ballotResult.ResetAllSubTotals(VotingDataSource.Conventional);
            ballotResult.ResetAllSubTotals(VotingDataSource.EVoting);

            await db.SaveChangesAsync();
        });
    }
}
