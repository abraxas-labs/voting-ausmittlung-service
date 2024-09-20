// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.ProportionalElectionResultTests;

public abstract class ProportionalElectionResultBaseTest : PoliticalBusinessResultBaseTest<
    ProportionalElectionResultService.ProportionalElectionResultServiceClient>
{
    protected ProportionalElectionResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override Task SeedPoliticalBusinessMockedData()
        => ProportionalElectionMockedData.Seed(RunScoped);

    protected async Task AssertCurrentState(CountingCircleResultState expectedState)
    {
        (await GetCurrentState()).Should().Be(expectedState);
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

    protected async Task SeedBallots(BallotBundleState bundleState)
    {
        await ErfassungElectionAdminClient.EnterCountOfVotersAsync(
            new EnterProportionalElectionCountOfVotersRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
                CountOfVoters = new EnterPoliticalBusinessCountOfVotersRequest
                {
                    ConventionalReceivedBallots = 1,
                    ConventionalAccountedBallots = 1,
                },
            });
        await RunEvents<ProportionalElectionResultCountOfVotersEntered>();
        await ProportionalElectionResultBundleMockedData.Seed(RunScoped);
        await RunOnDb(async db =>
        {
            var bundles = await db.ProportionalElectionBundles.AsTracking().ToListAsync();
            var bundle1 = bundles[0];
            bundle1.State = bundleState;
            bundle1.Ballots.Add(new ProportionalElectionResultBallot
            {
                BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1),
            });
            bundle1.Ballots.Add(new ProportionalElectionResultBallot
            {
                BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle1),
            });

            var bundle2 = bundles[1];
            bundle2.State = bundleState;
            bundle2.Ballots.Add(new ProportionalElectionResultBallot
            {
                BundleId = Guid.Parse(ProportionalElectionResultBundleMockedData.IdGossauBundle2),
            });
            await db.SaveChangesAsync();
        });
    }

    protected async Task RunToPublished()
    {
        var proportionalElectionResultAggregate = await AggregateRepositoryMock.GetOrCreateById<ProportionalElectionResultAggregate>(Guid.Parse(ProportionalElectionResultMockedData.IdGossauElectionResultInContestStGallen));
        if (proportionalElectionResultAggregate.Published)
        {
            return;
        }

        await RunOnResult<ProportionalElectionResultPublished>(aggregate => aggregate.Publish(ContestMockedData.StGallenEvotingUrnengang.Id));
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
            var aggregate = await aggregateRepository.GetOrCreateById<ProportionalElectionResultAggregate>(
                ProportionalElectionResultMockedData.GossauElectionResultInContestStGallen.Id);
            resultAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }
}
