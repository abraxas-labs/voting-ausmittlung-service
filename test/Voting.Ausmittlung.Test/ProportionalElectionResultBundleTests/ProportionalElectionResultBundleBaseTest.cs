// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Ausmittlung.Test.ProportionalElectionResultTests;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultBundleTests;

public abstract class ProportionalElectionResultBundleBaseTest : ProportionalElectionResultBaseTest
{
    protected ProportionalElectionResultBundleBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleMonitoringElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleErfassungElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleErfassungCreatorClient { get; private set; } = null!; // initialized during InitializeAsync

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleErfassungElectionAdminClientSecondUser { get; private set; } = null!; // initialized during InitializeAsync

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleErfassungCreatorClientSecondUser { get; private set; } = null!; // initialized during InitializeAsync

    protected ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient BundleErfassungElectionAdminClientStGallen { get; private set; } = null!; // initialized during InitializeAsync

    protected int LatestBallotNumber { get; private set; }

    public override async Task InitializeAsync()
    {
        BundleMonitoringElectionAdminClient = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.MonitoringElectionAdmin));
        BundleErfassungElectionAdminClient = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin));
        BundleErfassungCreatorClient = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungCreator));
        BundleErfassungElectionAdminClientSecondUser = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        BundleErfassungCreatorClientSecondUser = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, "my-user-99", RolesMockedData.ErfassungCreator));
        BundleErfassungElectionAdminClientStGallen = new ProportionalElectionResultBundleService.ProportionalElectionResultBundleServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, "my-user-99", RolesMockedData.ErfassungElectionAdmin));
        await base.InitializeAsync();
        await RunToState(CountingCircleResultState.SubmissionOngoing);
    }

    protected override async Task SeedPoliticalBusinessMockedData()
    {
        await base.SeedPoliticalBusinessMockedData();
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
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
            .BundleSubmissionFinishedAsync(new ProportionalElectionResultBundleSubmissionFinishedRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<ProportionalElectionResultBundleSubmissionFinished>();
    }

    protected async Task SetBundleInCorrection()
    {
        await BundleErfassungCreatorClientSecondUser
            .RejectBundleReviewAsync(new RejectProportionalElectionBundleReviewRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<ProportionalElectionResultBundleReviewRejected>();
    }

    protected async Task SetBundleReviewed()
    {
        await BundleErfassungCreatorClientSecondUser
            .SucceedBundleReviewAsync(new SucceedProportionalElectionBundleReviewRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<ProportionalElectionResultBundleReviewSucceeded>();
    }

    protected async Task SetBundleDeleted()
    {
        await BundleErfassungElectionAdminClient
            .DeleteBundleAsync(new DeleteProportionalElectionResultBundleRequest
            {
                BundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1,
            });
        await RunEvents<ProportionalElectionResultBundleDeleted>();
    }

    protected async Task CreateBallot(string bundleId = ProportionalElectionResultBundleMockedData.IdGossauBundle1)
    {
        var response = await BundleErfassungCreatorClient.CreateBallotAsync(new CreateProportionalElectionResultBallotRequest
        {
            BundleId = bundleId,
            Candidates =
                {
                    new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        Position = 1,
                        CandidateId = ProportionalElectionMockedData.CandidateId1GossauProportionalElectionInContestStGallen,
                        OnList = true,
                    },
                    new CreateUpdateProportionalElectionResultBallotCandidateRequest
                    {
                        Position = 2,
                        CandidateId = ProportionalElectionMockedData.CandidateId3GossauProportionalElectionInContestStGallen,
                    },
                },
            EmptyVoteCount = 1,
        });
        LatestBallotNumber = response.BallotNumber;
        await RunEvents<ProportionalElectionResultBallotCreated>();
    }

    protected Task<ProportionalElectionResultBundle> GetBundle(Guid? id = null)
    {
        id ??= Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1);
        return RunOnDb(db =>
            db.ProportionalElectionBundles
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
}
