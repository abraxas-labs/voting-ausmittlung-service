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
using Grpc.Net.Client;
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
using MajorityElectionResultEntryParams = Voting.Ausmittlung.Core.Domain.MajorityElectionResultEntryParams;
using SecondaryMajorityElectionResultBallot = Voting.Ausmittlung.Core.Domain.SecondaryMajorityElectionResultBallot;

namespace Voting.Ausmittlung.Test.MajorityElectionResultBundleTests;

public abstract class MajorityElectionResultBundleBaseTest
    : PoliticalBusinessResultBaseTest<MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient>
{
    protected MajorityElectionResultBundleBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient BundleErfassungElectionAdminClientBund { get; private set; } = null!; // initialized during InitializeAsync

    protected int LatestBallotNumber { get; private set; }

    public override async Task InitializeAsync()
    {
        BundleErfassungElectionAdminClientBund = new MajorityElectionResultBundleService.MajorityElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
        await RunEvents<MajorityElectionResultSubmissionStarted>();
        EventPublisherMock.Clear();
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await MajorityElectionMockedData.Seed(RunScoped);
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
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
        await RunOnBundle<MajorityElectionResultBundleSubmissionFinished>(
            bundleId,
            aggregate =>
            {
                switch (aggregate.State)
                {
                    case BallotBundleState.InCorrection:
                        aggregate.CorrectionFinished(ContestMockedData.Bundesurnengang.Id);
                        break;
                    case BallotBundleState.InProcess:
                        aggregate.SubmissionFinished(ContestMockedData.Bundesurnengang.Id);
                        break;
                }
            },
            userId);
    }

    protected async Task<Guid> CreateBundle(int bundleNumber, string userId = TestDefaults.UserId)
    {
        var bundleId = Guid.NewGuid();
        await RunOnBundle<MajorityElectionResultBundleCreated>(
            bundleId,
            aggregate =>
            {
                aggregate.Create(
                    bundleId,
                    MajorityElectionResultMockedData.StGallenElectionResultInContestBund.Id,
                    bundleNumber,
                    MajorityElectionResultEntry.Detailed,
                    new MajorityElectionResultEntryParams
                    {
                        ReviewProcedure = MajorityElectionReviewProcedure.Physically,
                        AutomaticBallotBundleNumberGeneration = true,
                        AutomaticBallotNumberGeneration = true,
                        BallotBundleSize = 10,
                        BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
                        CandidateCheckDigit = false,
                        AutomaticEmptyVoteCounting = false,
                        BallotBundleSampleSize = 1,
                    },
                    ContestMockedData.Bundesurnengang.Id);
            },
            userId);
        return bundleId;
    }

    protected async Task SetBundleInCorrection(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<MajorityElectionResultBundleReviewRejected>(
            bundleId,
            aggregate =>
            {
                if (aggregate.State == BallotBundleState.ReadyForReview)
                {
                    aggregate.RejectReview(ContestMockedData.Bundesurnengang.Id);
                }
            },
            userId);
    }

    protected async Task SetBundleReviewed(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<MajorityElectionResultBundleReviewSucceeded>(
            bundleId,
            aggregate => aggregate.SucceedReview(ContestMockedData.Bundesurnengang.Id),
            userId);
    }

    protected async Task SetBundleDeleted(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<MajorityElectionResultBundleDeleted>(
            bundleId,
            aggregate => aggregate.Delete(ContestMockedData.Bundesurnengang.Id),
            userId);
    }

    protected async Task CreateBallot(Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<MajorityElectionResultBallotCreated>(
            bundleId,
            aggregate =>
            {
                if (aggregate.State != BallotBundleState.InProcess && aggregate.State != BallotBundleState.InCorrection)
                {
                    return;
                }

                aggregate.CreateBallot(
                    null,
                    0,
                    0,
                    0,
                    new List<Guid>
                    {
                        Guid.Parse(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund),
                    },
                    new List<SecondaryMajorityElectionResultBallot>
                    {
                        new()
                        {
                            EmptyVoteCount = 1,
                            IndividualVoteCount = 1,
                            SelectedCandidateIds = new List<Guid>
                            {
                                Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                            },
                            SecondaryMajorityElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
                        },
                    },
                    ContestMockedData.Bundesurnengang.Id);
                LatestBallotNumber = aggregate.CurrentBallotNumber;
            },
            userId);
    }

    protected async Task UpdateBallot(int ballotNumber, Guid? bundleId = null, string userId = TestDefaults.UserId)
    {
        await RunOnBundle<MajorityElectionResultBallotUpdated>(
            bundleId,
            aggregate =>
            {
                aggregate.UpdateBallot(
                    ballotNumber,
                    0,
                    0,
                    0,
                    new List<Guid>
                    {
                        Guid.Parse(MajorityElectionMockedData.CandidateId2StGallenMajorityElectionInContestBund),
                    },
                    new List<SecondaryMajorityElectionResultBallot>
                    {
                        new()
                        {
                            EmptyVoteCount = 1,
                            IndividualVoteCount = 1,
                            SelectedCandidateIds = new List<Guid>
                            {
                                Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund),
                            },
                            SecondaryMajorityElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
                        },
                    },
                    ContestMockedData.Bundesurnengang.Id);
            },
            userId);
    }

    protected Task<MajorityElectionResultBundle> GetBundle(Guid? id = null)
    {
        id ??= Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1);
        return RunOnDb(db =>
            db.MajorityElectionResultBundles
                .Include(x => x.Logs)
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

    protected override async Task<CountingCircleResultState> GetCurrentState()
    {
        var aggregate = await AggregateRepositoryMock.GetOrCreateById<MajorityElectionResultAggregate>(
            Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund));
        return aggregate.State;
    }

    protected override async Task SetPlausibilised()
    {
        await RunOnResult<MajorityElectionResultPlausibilised>(aggregate =>
            aggregate.Plausibilise(ContestMockedData.Bundesurnengang.Id));
    }

    protected override async Task SetAuditedTentatively()
    {
        await RunOnResult<MajorityElectionResultAuditedTentatively>(aggregate =>
            aggregate.AuditedTentatively(ContestMockedData.Bundesurnengang.Id));
    }

    protected override async Task SetCorrectionDone()
    {
        await RunOnResult<MajorityElectionResultCorrectionFinished>(aggregate =>
            aggregate.CorrectionFinished(string.Empty, ContestMockedData.Bundesurnengang.Id));
    }

    protected override async Task SetReadyForCorrection()
    {
        await RunOnResult<MajorityElectionResultFlaggedForCorrection>(aggregate =>
            aggregate.FlagForCorrection(ContestMockedData.Bundesurnengang.Id));
    }

    protected override async Task SetSubmissionDone()
    {
        await RunOnResult<MajorityElectionResultSubmissionFinished>(aggregate =>
            aggregate.SubmissionFinished(ContestMockedData.Bundesurnengang.Id));
    }

    protected override async Task SetSubmissionOngoing()
    {
        await RunOnResult<MajorityElectionResultSubmissionStarted>(aggregate =>
            aggregate.StartSubmission(
                CountingCircleMockedData.StGallen.Id,
                MajorityElectionMockedData.StGallenMajorityElectionInContestBund.Id,
                ContestMockedData.Bundesurnengang.Id,
                false));
    }

    protected async Task DefineResultEntry()
    {
        await RunOnResult<MajorityElectionResultEntryDefined>(aggregate => aggregate.DefineEntry(
            MajorityElectionResultEntry.Detailed,
            ContestMockedData.Bundesurnengang.Id,
            new MajorityElectionResultEntryParams
            {
                ReviewProcedure = MajorityElectionReviewProcedure.Physically,
                AutomaticBallotBundleNumberGeneration = true,
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.RestartForEachBundle,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 2,
                CandidateCheckDigit = false,
            }));
    }

    protected async Task<int> GenerateBundleNumber()
    {
        var bundleNumber = 0;

        await RunOnResult<MajorityElectionResultBundleNumberEntered>(aggregate =>
            bundleNumber = aggregate.GenerateBundleNumber(ContestMockedData.Bundesurnengang.Id));

        return bundleNumber;
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

    protected async Task ReplaceNullValuesWithZeroOnDetailedResults()
    {
        var resultIds = new List<Guid>
        {
            Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund),
            Guid.Parse(MajorityElectionResultMockedData.IdUzwilElectionResultInContestStGallen),
        };

        await RunOnDb(async db =>
        {
            var results = await db.MajorityElectionResults
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                .Include(x => x.CandidateResults)
                .Where(x => resultIds.Contains(x.Id))
                .ToListAsync();

            foreach (var result in results)
            {
                result.ConventionalSubTotal.ReplaceNullValuesWithZero();

                foreach (var candidateResult in result.CandidateResults.OfType<MajorityElectionCandidateResultBase>().Concat(result.SecondaryMajorityElectionResults.SelectMany(x => x.CandidateResults)))
                {
                    candidateResult.ConventionalVoteCount ??= 0;
                }

                foreach (var smer in result.SecondaryMajorityElectionResults)
                {
                    smer.ConventionalSubTotal.ReplaceNullValuesWithZero();
                }
            }

            await db.SaveChangesAsync();
        });
    }

    private async Task RunOnBundle<T>(Guid? bundleId, Action<MajorityElectionResultBundleAggregate> bundleAction, string userId = TestDefaults.UserId)
        where T : IMessage<T>
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            var actualBundleId = bundleId ?? Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1);

            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", userId, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetOrCreateById<MajorityElectionResultBundleAggregate>(actualBundleId);
            bundleAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }

    private async Task RunOnResult<T>(Action<MajorityElectionResultAggregate> resultAction)
        where T : IMessage<T>
    {
        await RunScoped<IServiceProvider>(async sp =>
        {
            // needed to create aggregates, since they access user/tenant information
            var authStore = sp.GetRequiredService<IAuthStore>();
            authStore.SetValues("mock-token", SecureConnectTestDefaults.MockedUserDefault.Loginid, "test", Enumerable.Empty<string>());

            var aggregateRepository = sp.GetRequiredService<IAggregateRepository>();
            var aggregate = await aggregateRepository.GetOrCreateById<MajorityElectionResultAggregate>(
                MajorityElectionResultMockedData.StGallenElectionResultInContestBund.Id);
            resultAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }
}
