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
using ProportionalElectionResultBallotCandidate = Voting.Ausmittlung.Core.Domain.ProportionalElectionResultBallotCandidate;
using ProportionalElectionResultEntryParams = Voting.Ausmittlung.Core.Domain.ProportionalElectionResultEntryParams;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public abstract class ProportionalElectionResultBundleBaseTest
    : PoliticalBusinessResultBaseTest<ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient>
{
    protected ProportionalElectionResultBundleBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleErfassungElectionAdminClientStGallen { get; private set; } = null!; // initialized during InitializeAsync

    protected int LatestBallotNumber { get; private set; }

    public override async Task InitializeAsync()
    {
        BundleErfassungElectionAdminClientStGallen = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await RunEvents<ProportionalElectionResultSubmissionStarted>();
        EventPublisherMock.Clear();
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await ProportionalElectionMockedData.Seed(RunScoped);
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
    }

    protected async Task RunBundleToState(BallotBundleState state, Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        switch (state)
        {
            case BallotBundleState.InCorrection:
                await RunBundleToState(BallotBundleState.ReadyForReview, bundleId, userId);
                await SetBundleInCorrection(bundleId, userId);
                break;
            case BallotBundleState.ReadyForReview:
                await CreateBallot(bundleId, userId);
                await SetBundleSubmissionFinished(bundleId, userId);
                break;
            case BallotBundleState.Reviewed:
                await RunBundleToState(BallotBundleState.ReadyForReview, bundleId, userId);
                await SetBundleReviewed(bundleId, userId);
                break;
            case BallotBundleState.Deleted:
                await SetBundleDeleted(bundleId, userId);
                break;
        }
    }

    protected async Task SetBundleSubmissionFinished(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<ProportionalElectionResultBundleSubmissionFinished>(
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
            },
            userId);
    }

    protected async Task<Guid> CreateBundle(int bundleNumber, string userId = TestDefaults.UserId)
    {
        var bundleId = Guid.NewGuid();
        await RunOnBundle<ProportionalElectionResultBundleCreated>(
            bundleId,
            aggregate =>
            {
                aggregate.Create(
                    bundleId,
                    ProportionalElectionResultMockedData.GossauElectionResultInContestStGallen.Id,
                    null,
                    bundleNumber,
                    new ProportionalElectionResultEntryParams
                    {
                        ReviewProcedure = ProportionalElectionReviewProcedure.Physically,
                        AutomaticBallotBundleNumberGeneration = true,
                        AutomaticBallotNumberGeneration = true,
                        BallotBundleSize = 10,
                        BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
                        CandidateCheckDigit = false,
                        AutomaticEmptyVoteCounting = false,
                        BallotBundleSampleSize = 1,
                    },
                    ContestMockedData.StGallenEvotingUrnengang.Id);
            },
            userId);
        return bundleId;
    }

    protected async Task SetBundleInCorrection(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<ProportionalElectionResultBundleReviewRejected>(
            bundleId,
            aggregate =>
            {
                if (aggregate.State == BallotBundleState.ReadyForReview)
                {
                    aggregate.RejectReview(ContestMockedData.StGallenEvotingUrnengang.Id);
                }
            },
            userId);
    }

    protected async Task SetBundleReviewed(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<ProportionalElectionResultBundleReviewSucceeded>(
            bundleId,
            aggregate => aggregate.SucceedReview(ContestMockedData.StGallenEvotingUrnengang.Id),
            userId);
    }

    protected async Task SetBundleDeleted(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<ProportionalElectionResultBundleDeleted>(
            bundleId,
            aggregate => aggregate.Delete(ContestMockedData.StGallenEvotingUrnengang.Id),
            userId);
    }

    protected override async Task<CountingCircleResultState> GetCurrentState()
    {
        var aggregate = await AggregateRepositoryMock.GetOrCreateById<ProportionalElectionResultAggregate>(
            Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen));
        return aggregate.State;
    }

    protected override async Task SetPlausibilised()
    {
        await RunOnResult<ProportionalElectionResultPlausibilised>(aggregate =>
            aggregate.Plausibilise(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetAuditedTentatively()
    {
        await RunOnResult<ProportionalElectionResultAuditedTentatively>(aggregate =>
            aggregate.AuditedTentatively(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetCorrectionDone()
    {
        await RunOnResult<ProportionalElectionResultCorrectionFinished>(aggregate =>
            aggregate.CorrectionFinished(string.Empty, ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetReadyForCorrection()
    {
        await RunOnResult<ProportionalElectionResultFlaggedForCorrection>(aggregate =>
            aggregate.FlagForCorrection(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetSubmissionDone()
    {
        await RunOnResult<ProportionalElectionResultSubmissionFinished>(aggregate =>
            aggregate.SubmissionFinished(ContestMockedData.StGallenEvotingUrnengang.Id));
    }

    protected override async Task SetSubmissionOngoing()
    {
        await RunOnResult<ProportionalElectionResultSubmissionStarted>(aggregate =>
            aggregate.StartSubmission(
                CountingCircleMockedData.Gossau.Id,
                ProportionalElectionMockedData.GossauProportionalElectionInContestStGallen.Id,
                ContestMockedData.StGallenEvotingUrnengang.Id,
                false));
    }

    protected async Task CreateBallot(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<ProportionalElectionResultBallotCreated>(
            bundleId,
            aggregate =>
            {
                if (aggregate.State != BallotBundleState.InProcess && aggregate.State != BallotBundleState.InCorrection)
                {
                    return;
                }

                aggregate.CreateBallot(
                    null,
                    1,
                    new List<ProportionalElectionResultBallotCandidate>
                    {
                        new()
                        {
                            Position = 1,
                            CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen),
                            OnList = true,
                        },
                        new()
                        {
                            Position = 2,
                            CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId3GossauProportionalElectionInContestStGallen),
                        },
                    },
                    ContestMockedData.StGallenEvotingUrnengang.Id);
                LatestBallotNumber = aggregate.CurrentBallotNumber;
            },
            userId);
    }

    protected async Task UpdateBallot(int ballotNumber, Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<ProportionalElectionResultBallotUpdated>(
            bundleId,
            aggregate =>
            {
                aggregate.UpdateBallot(
                    ballotNumber,
                    1,
                    new List<ProportionalElectionResultBallotCandidate>
                    {
                        new()
                        {
                            Position = 1,
                            CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen),
                            OnList = true,
                        },
                        new()
                        {
                            Position = 2,
                            CandidateId = Guid.Parse(ProportionalElectionMockedData.CandidateId3GossauProportionalElectionInContestStGallen),
                        },
                    },
                    ContestMockedData.StGallenEvotingUrnengang.Id);
            },
            userId);
    }

    protected Task<ProportionalElectionResultBundle> GetBundle(Guid? id = null)
    {
        id ??= Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1);
        return RunOnDb(db =>
            db.ProportionalElectionBundles
                .Include(x => x.Logs)
                .Include(x => x.ElectionResult)
                .Where(x => x.Id == id)
                .FirstAsync());
    }

    protected Task<ProportionalElectionResult> GetElectionResult(Guid? id = null)
    {
        id ??= ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen;
        return RunOnDb(db =>
            db.ProportionalElectionResults
                .Where(x => x.Id == id)
                .FirstAsync());
    }

    protected async Task ShouldHaveCandidateResults(bool shouldHaveResults)
    {
        var hasNonZeroCandidateResults = await RunOnDb(db => db.ProportionalElectionCandidateResults
            .AnyAsync(c =>
                c.ListResult.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen &&
                (c.ConventionalSubTotal.ModifiedListVotesCount != 0 || c.ConventionalSubTotal.CountOfVotesOnOtherLists != 0 || c.ConventionalSubTotal.CountOfVotesFromAccumulations != 0)));
        hasNonZeroCandidateResults.Should().Be(shouldHaveResults);
    }

    protected async Task ShouldHaveListResults(bool shouldHaveResults)
    {
        var hasNonZeroListResults = await RunOnDb(db => db.ProportionalElectionListResults
            .AnyAsync(c =>
                c.ResultId == ProportionalElectionResultMockedData.GuidGossauElectionResultInContestStGallen &&
                (c.ConventionalSubTotal.ModifiedListsCount != 0 || c.ConventionalSubTotal.ModifiedListVotesCount != 0 || c.ConventionalSubTotal.ModifiedListBlankRowsCount != 0)));
        hasNonZeroListResults.Should().Be(shouldHaveResults);
    }

    private async Task RunOnBundle<T>(Guid? bundleId, Action<ProportionalElectionResultBundleAggregate> bundleAction, string userId = TestDefaults.UserId)
        where T : IMessage<T>
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            var actualBundleId = bundleId ?? Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1);

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", userId, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetOrCreateById<ProportionalElectionResultBundleAggregate>(actualBundleId);
            bundleAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }

    private async Task RunOnResult<T>(Action<ProportionalElectionResultAggregate> resultAction)
        where T : IMessage<T>
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetById<ProportionalElectionResultAggregate>(ProportionalElectionResultMockedData.GossauElectionResultInContestStGallen.Id);
            resultAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }
}
