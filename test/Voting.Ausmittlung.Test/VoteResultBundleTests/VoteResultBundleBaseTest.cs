// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;
using VoteResultBallotQuestionAnswer = Voting.Ausmittlung.Core.Domain.VoteResultBallotQuestionAnswer;
using VoteResultBallotTieBreakQuestionAnswer = Voting.Ausmittlung.Core.Domain.VoteResultBallotTieBreakQuestionAnswer;
using VoteResultEntryParams = Voting.Ausmittlung.Core.Domain.VoteResultEntryParams;

namespace Voting.Ausmittlung.Test.VoteResultBundleTests;

public abstract class VoteResultBundleBaseTest : PoliticalBusinessResultBaseTest<VoteResultBundleService.VoteResultBundleServiceClient>
{
    protected VoteResultBundleBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected VoteResultBundleService.VoteResultBundleServiceClient BundleErfassungElectionAdminClientStGallen { get; private set; } =
        null!; // initialized during InitializeAsync

    protected int LatestBallotNumber { get; private set; }

    public override async Task InitializeAsync()
    {
        BundleErfassungElectionAdminClientStGallen = new VoteResultBundleService.VoteResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await ResetQuestionResults();
        EventPublisherMock.Clear();
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await VoteMockedData.Seed(RunScoped);
        await VoteResultBundleMockedData.Seed(RunScoped);
    }

    protected async Task RunBundleToState(BallotBundleState state, Guid? bundleId = null)
    {
        switch (state)
        {
            case BallotBundleState.InCorrection:
                await RunBundleToState(BallotBundleState.ReadyForReview, bundleId);
                await SetBundleInCorrection(bundleId);
                break;
            case BallotBundleState.ReadyForReview:
                await CreateBallot(bundleId);
                await SetBundleSubmissionFinished(bundleId);
                break;
            case BallotBundleState.Reviewed:
                await RunBundleToState(BallotBundleState.ReadyForReview, bundleId);
                await SetBundleReviewed(bundleId);
                break;
            case BallotBundleState.Deleted:
                await SetBundleDeleted(bundleId);
                break;
        }
    }

    protected async Task SetBundleSubmissionFinished(Guid? bundleId = null)
    {
        await RunOnBundle<VoteResultBundleSubmissionFinished>(
            bundleId,
            aggregate =>
            {
                switch (aggregate.State)
                {
                    case BallotBundleState.InCorrection:
                        aggregate.CorrectionFinished(ContestMockedData.StGallenEvotingUrnengang.Id);
                        break;
                    case BallotBundleState.InProcess:
                        aggregate.SubmissionFinished(ContestMockedData.StGallenEvotingUrnengang.Id);
                        break;
                }
            });
    }

    protected async Task<Guid> CreateBundle(int bundleNumber, string userId = TestDefaults.UserId)
    {
        var bundleId = Guid.NewGuid();
        await RunOnBundle<VoteResultBundleCreated>(
            bundleId,
            aggregate =>
            {
                aggregate.Create(
                    bundleId,
                    VoteResultMockedData.GossauVoteInContestStGallenResult.Id,
                    Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenBallotResult),
                    bundleNumber,
                    new VoteResultEntryParams
                    {
                        BallotBundleSampleSizePercent = 10,
                        ReviewProcedure = VoteReviewProcedure.Physically,
                        AutomaticBallotBundleNumberGeneration = true,
                    },
                    ContestMockedData.StGallenEvotingUrnengang.Id);
            },
            userId);
        return bundleId;
    }

    protected async Task SetBundleInCorrection(Guid? bundleId = null)
    {
        await RunOnBundle<VoteResultBundleReviewRejected>(
            bundleId,
            aggregate =>
            {
                if (aggregate.State == BallotBundleState.ReadyForReview)
                {
                    aggregate.RejectReview(ContestMockedData.StGallenEvotingUrnengang.Id);
                }
            });
    }

    protected async Task SetBundleReviewed(Guid? bundleId = null)
    {
        await RunOnBundle<VoteResultBundleReviewSucceeded>(bundleId, aggregate => aggregate.SucceedReview(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected async Task SetBundleDeleted(Guid? bundleId = null)
    {
        await RunOnBundle<VoteResultBundleDeleted>(bundleId, aggregate => aggregate.Delete(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected async Task CreateBallot(Guid? bundleId = null)
    {
        await RunOnBundle<VoteResultBallotCreated>(bundleId, aggregate =>
        {
            if (aggregate.State != BallotBundleState.InProcess && aggregate.State != BallotBundleState.InCorrection)
            {
                return;
            }

            aggregate.CreateBallot(
                new List<VoteResultBallotQuestionAnswer>
                {
                    new() { QuestionNumber = 1, Answer = BallotQuestionAnswer.Yes, },
                    new() { QuestionNumber = 2, Answer = BallotQuestionAnswer.No, },
                },
                new List<VoteResultBallotTieBreakQuestionAnswer>
                {
                    new() { QuestionNumber = 1, Answer = TieBreakQuestionAnswer.Q1, },
                },
                ContestMockedData.StGallenEvotingUrnengang.Id);
            LatestBallotNumber = aggregate.CurrentBallotNumber;
        });
    }

    protected Task<VoteResultBundle> GetBundle(Guid? id = null)
    {
        id ??= Guid.Parse(VoteResultBundleMockedData.IdGossauBundle1);
        return RunOnDb(db =>
            db.VoteResultBundles
                .Include(x => x.Logs)
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

    protected override async Task<CountingCircleResultState> GetCurrentState()
    {
        var aggregate = await AggregateRepositoryMock.GetOrCreateById<VoteResultAggregate>(Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult));
        return aggregate.State;
    }

    protected override async Task SetPlausibilised()
    {
        await RunOnResult<VoteResultPlausibilised>(aggregate =>
            aggregate.Plausibilise(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetAuditedTentatively()
    {
        await RunOnResult<VoteResultAuditedTentatively>(aggregate =>
            aggregate.AuditedTentatively(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetCorrectionDone()
    {
        await RunOnResult<VoteResultCorrectionFinished>(aggregate =>
            aggregate.CorrectionFinished(string.Empty, ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetReadyForCorrection()
    {
        await RunOnResult<VoteResultFlaggedForCorrection>(aggregate =>
            aggregate.FlagForCorrection(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetSubmissionDone()
    {
        await RunOnResult<VoteResultSubmissionFinished>(aggregate =>
            aggregate.SubmissionFinished(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetSubmissionOngoing()
    {
        await RunOnResult<VoteResultSubmissionStarted>(aggregate =>
            aggregate.StartSubmission(CountingCircleMockedData.Gossau.Id, VoteMockedData.GossauVoteInContestStGallen.Id, ContestMockedData.StGallenEvotingUrnengang.Id, false));
    }

    private async Task RunOnBundle<T>(Guid? bundleId, Action<VoteResultBundleAggregate> bundleAction, string userId = TestDefaults.UserId)
        where T : IMessage<T>
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            var actualBundleId = bundleId ?? VoteResultBundleMockedData.GossauBundle1.Id;

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", userId, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetOrCreateById<VoteResultBundleAggregate>(actualBundleId);
            bundleAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }

    private async Task RunOnResult<T>(Action<VoteResultAggregate> resultAction)
        where T : IMessage<T>
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetById<VoteResultAggregate>(VoteResultMockedData.GossauVoteInContestStGallenResult.Id);
            resultAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
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
