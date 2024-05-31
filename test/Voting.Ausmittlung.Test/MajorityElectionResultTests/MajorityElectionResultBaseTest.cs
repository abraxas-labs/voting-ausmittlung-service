// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
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

namespace Voting.Ausmittlung.Test.MajorityElectionResultTests;

public abstract class MajorityElectionResultBaseTest : PoliticalBusinessResultBaseTest<
    MajorityElectionResultService.MajorityElectionResultServiceClient>
{
    protected MajorityElectionResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected MajorityElectionResultService.MajorityElectionResultServiceClient BundErfassungElectionAdminClient { get; private set; } = null!; // initialized during InitializeAsync

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ReplaceNullValuesWithZeroOnDetailedResults();
        BundErfassungElectionAdminClient = new MajorityElectionResultService.MajorityElectionResultServiceClient(CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantBund.Id, TestDefaults.UserId, RolesMockedData.ErfassungElectionAdmin));
    }

    protected override Task SeedPoliticalBusinessMockedData()
        => MajorityElectionMockedData.Seed(RunScoped);

    protected async Task AssertCurrentState(CountingCircleResultState expectedState)
    {
        (await GetCurrentState()).Should().Be(expectedState);
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

    protected async Task SeedBallots(BallotBundleState bundleState)
    {
        await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
            new EnterMajorityElectionCountOfVotersRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                {
                    ConventionalReceivedBallots = 1,
                    ConventionalAccountedBallots = 1,
                },
            });
        await RunEvents<MajorityElectionResultCountOfVotersEntered>();
        await MajorityElectionResultBundleMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var bundles = await db.MajorityElectionResultBundles.AsTracking().ToListAsync();
            var bundle1 = bundles[0];
            bundle1.State = bundleState;
            bundle1.Ballots.Add(new MajorityElectionResultBallot
            {
                BundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
            });
            bundle1.Ballots.Add(new MajorityElectionResultBallot
            {
                BundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle1),
            });

            var bundle2 = bundles[1];
            bundle2.State = bundleState;
            bundle2.Ballots.Add(new MajorityElectionResultBallot
            {
                BundleId = Guid.Parse(MajorityElectionResultBundleMockedData.IdStGallenBundle2),
            });
            await db.SaveChangesAsync();
        });
    }

    protected async Task RunToPublished()
    {
        var majorityElectionResultAggregate = await AggregateRepositoryMock.GetOrCreateById<MajorityElectionResultAggregate>(Guid.Parse(MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund));
        if (majorityElectionResultAggregate.Published)
        {
            return;
        }

        await RunOnResult<MajorityElectionResultPublished>(aggregate => aggregate.Publish(ContestMockedData.GuidBundesurnengang));
    }

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

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

    private async Task ReplaceNullValuesWithZeroOnDetailedResults()
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
}
