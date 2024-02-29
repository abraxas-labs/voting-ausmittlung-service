// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.EventProcessors;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Iam.Testing.AuthenticationScheme;
using Voting.Lib.Testing;

namespace Voting.Ausmittlung.Test;

public abstract class PoliticalBusinessResultBaseTest<T> : BaseTest<T>
    where T : ClientBase<T>
{
    protected PoliticalBusinessResultBaseTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await ContestMockedData.Seed(RunScoped);
        await SeedPoliticalBusinessMockedData();
        await SecondFactorTransactionMockedData.Seed(RunScoped);
        await RunScoped((DomainOfInfluencePermissionBuilder permissionBuilder) =>
            permissionBuilder.RebuildPermissionTree());
    }

    protected abstract Task SeedPoliticalBusinessMockedData();

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantGossau.Id, TestDefaults.UserId, roles);

    protected abstract Task<CountingCircleResultState> GetCurrentState();

    protected async Task RunToState(
        CountingCircleResultState targetState,
        CountingCircleResultState? currentState = null)
    {
        currentState ??= await GetCurrentState();
        if (currentState >= targetState)
        {
            return;
        }

        switch (targetState)
        {
            case CountingCircleResultState.SubmissionOngoing:
                await SetSubmissionOngoing();
                break;
            case CountingCircleResultState.SubmissionDone:
                await RunToState(CountingCircleResultState.SubmissionOngoing, currentState);
                await SetSubmissionDone();
                break;
            case CountingCircleResultState.ReadyForCorrection:
                await RunToState(CountingCircleResultState.SubmissionDone, currentState);
                await SetReadyForCorrection();
                break;
            case CountingCircleResultState.CorrectionDone:
                await RunToState(CountingCircleResultState.ReadyForCorrection, currentState);
                await SetCorrectionDone();
                break;
            case CountingCircleResultState.AuditedTentatively:
                await RunToState(CountingCircleResultState.CorrectionDone, currentState);
                await SetAuditedTentatively();
                break;
            case CountingCircleResultState.Plausibilised:
                await RunToState(CountingCircleResultState.AuditedTentatively, currentState);
                await SetPlausibilised();
                break;
        }
    }

    protected abstract Task SetPlausibilised();

    protected abstract Task SetAuditedTentatively();

    protected abstract Task SetCorrectionDone();

    protected abstract Task SetReadyForCorrection();

    protected abstract Task SetSubmissionDone();

    protected abstract Task SetSubmissionOngoing();

    protected Task SetReadModelResultStateForAllResultsInContest(Guid contestId, CountingCircleResultState state)
    {
        return RunOnDb(async db =>
        {
            var contest = await db.Contests
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.Votes)
                    .ThenInclude(x => x.Results)
                .Include(x => x.ProportionalElections)
                    .ThenInclude(x => x.Results)
                .Include(x => x.MajorityElections)
                    .ThenInclude(x => x.Results)
                    .FirstAsync(x => x.Id == contestId);

            var pbs = contest.Votes.OfType<PoliticalBusiness>().Concat(contest.ProportionalElections).Concat(contest.MajorityElections);
            foreach (var result in pbs.SelectMany(pb => pb.CountingCircleResults))
            {
                result.State = state;
            }

            await db.SaveChangesAsync();
        });
    }
}
