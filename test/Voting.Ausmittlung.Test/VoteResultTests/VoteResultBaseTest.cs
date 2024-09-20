// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Eventing.Persistence;
using Voting.Lib.Iam.Store;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Ausmittlung.Test.VoteResultTests;

public abstract class VoteResultBaseTest : PoliticalBusinessResultBaseTest<VoteResultService.VoteResultServiceClient>
{
    protected VoteResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    protected override Task SeedPoliticalBusinessMockedData()
        => VoteMockedData.Seed(RunScoped);

    protected async Task AssertCurrentState(CountingCircleResultState expectedState)
    {
        (await GetCurrentState()).Should().Be(expectedState);
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

    protected async Task RunToPublished()
    {
        var voteResultAggregate = await AggregateRepositoryMock.GetOrCreateById<VoteResultAggregate>(Guid.Parse(VoteResultMockedData.IdGossauVoteInContestStGallenResult));
        if (voteResultAggregate.Published)
        {
            return;
        }

        await RunOnResult<VoteResultPublished>(aggregate => aggregate.Publish(ContestMockedData.StGallenEvotingUrnengang.Id));
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
            var aggregate = await aggregateRepository.GetOrCreateById<VoteResultAggregate>(VoteResultMockedData.GossauVoteInContestStGallenResult.Id);
            resultAction(aggregate);
            await aggregateRepository.Save(aggregate);
        });
        await RunEvents<T>();
        EventPublisherMock.Clear();
    }
}
