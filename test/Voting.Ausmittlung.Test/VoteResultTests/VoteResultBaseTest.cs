// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;

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
        var result = await ErfassungCreatorClient.GetAsync(new GetVoteResultRequest
        {
            VoteId = VoteMockedData.IdGossauVoteInContestStGallen,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });
        return (CountingCircleResultState)result.State;
    }

    protected override async Task SetPlausibilised()
    {
        await MonitoringElectionAdminClient
            .PlausibiliseAsync(new VoteResultsPlausibiliseRequest
            {
                VoteResultIds =
                {
                        VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                },
            });
        await RunEvents<VoteResultPlausibilised>();
    }

    protected override async Task SetAuditedTentatively()
    {
        await MonitoringElectionAdminClient
            .AuditedTentativelyAsync(new VoteResultAuditedTentativelyRequest
            {
                VoteResultIds =
                {
                    VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                },
            });
        await RunEvents<VoteResultAuditedTentatively>();
    }

    protected override async Task SetCorrectionDone()
    {
        await ErfassungElectionAdminClient
            .CorrectionFinishedAsync(new VoteResultCorrectionFinishedRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });
        await RunEvents<VoteResultCorrectionFinished>();
    }

    protected override async Task SetReadyForCorrection()
    {
        await MonitoringElectionAdminClient
            .FlagForCorrectionAsync(new VoteResultFlagForCorrectionRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
            });
        await RunEvents<VoteResultFlaggedForCorrection>();
    }

    protected override async Task SetSubmissionDone()
    {
        await ErfassungElectionAdminClient
            .SubmissionFinishedAsync(new VoteResultSubmissionFinishedRequest
            {
                VoteResultId = VoteResultMockedData.IdGossauVoteInContestStGallenResult,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });
        await RunEvents<VoteResultSubmissionFinished>();
    }

    protected override async Task SetSubmissionOngoing()
    {
        var contestGuid = Guid.Parse(ContestMockedData.IdStGallenEvoting);
        var countingCircleGuid = CountingCircleMockedData.GuidGossau;
        await RunOnDb(async db =>
        {
            var ccDetails = await db.ContestCountingCircleDetails
                .AsTracking()
                .SingleAsync(x => x.ContestId == contestGuid && x.CountingCircle.BasisCountingCircleId == countingCircleGuid);
            await db.SaveChangesAsync();
        });

        await new ResultService.ResultServiceClient(CreateGrpcChannel(RolesMockedData.ErfassungElectionAdmin))
            .GetListAsync(new GetResultListRequest
            {
                ContestId = ContestMockedData.IdStGallenEvoting,
                CountingCircleId = CountingCircleMockedData.IdGossau,
            });
        await RunEvents<VoteResultSubmissionStarted>();
    }
}
