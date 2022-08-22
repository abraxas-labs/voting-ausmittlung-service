// (c) Copyright 2022 by Abraxas Informatik AG
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
        var result = await ErfassungCreatorClient.GetAsync(new GetProportionalElectionResultRequest
        {
            ElectionId = ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen,
            CountingCircleId = CountingCircleMockedData.IdGossau,
        });
        return (CountingCircleResultState)result.State;
    }

    protected override async Task SetPlausibilised()
    {
        await MonitoringElectionAdminClient
            .PlausibiliseAsync(new ProportionalElectionResultsPlausibiliseRequest
            {
                ElectionResultIds =
                {
                        ProportionalElectionResultMockedData
                            .IdGossauElectionResultInContestStGallen,
                },
            });
        await RunEvents<ProportionalElectionResultPlausibilised>();
    }

    protected override async Task SetAuditedTentatively()
    {
        await MonitoringElectionAdminClient
            .AuditedTentativelyAsync(new ProportionalElectionResultAuditedTentativelyRequest
            {
                ElectionResultIds =
                {
                        ProportionalElectionResultMockedData
                            .IdGossauElectionResultInContestStGallen,
                },
            });
        await RunEvents<ProportionalElectionResultAuditedTentatively>();
    }

    protected override async Task SetCorrectionDone()
    {
        await ErfassungElectionAdminClient
            .CorrectionFinishedAsync(new ProportionalElectionResultCorrectionFinishedRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });
        await RunEvents<ProportionalElectionResultCorrectionFinished>();
    }

    protected override async Task SetReadyForCorrection()
    {
        await MonitoringElectionAdminClient
            .FlagForCorrectionAsync(new ProportionalElectionResultFlagForCorrectionRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
            });
        await RunEvents<ProportionalElectionResultFlaggedForCorrection>();
    }

    protected override async Task SetSubmissionDone()
    {
        await ErfassungElectionAdminClient
            .SubmissionFinishedAsync(new ProportionalElectionResultSubmissionFinishedRequest
            {
                ElectionResultId = ProportionalElectionResultMockedData
                    .IdGossauElectionResultInContestStGallen,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });
        await RunEvents<ProportionalElectionResultSubmissionFinished>();
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
        await RunEvents<ProportionalElectionResultSubmissionStarted>();
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
}
