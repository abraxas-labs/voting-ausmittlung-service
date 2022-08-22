// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Services.V1;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Auth;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Test.MockedData;
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

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await ReplaceNullValuesWithZeroOnDetailedResults();
    }

    protected override Task SeedPoliticalBusinessMockedData()
        => MajorityElectionMockedData.Seed(RunScoped);

    protected async Task AssertCurrentState(CountingCircleResultState expectedState)
    {
        (await GetCurrentState()).Should().Be(expectedState);
    }

    protected override async Task<CountingCircleResultState> GetCurrentState()
    {
        var result = await ErfassungCreatorClient.GetAsync(new GetMajorityElectionResultRequest
        {
            ElectionId = MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund,
            CountingCircleId = CountingCircleMockedData.IdStGallen,
        });
        return (CountingCircleResultState)result.State;
    }

    protected override async Task SetPlausibilised()
    {
        await MonitoringElectionAdminClient
            .PlausibiliseAsync(new MajorityElectionResultsPlausibiliseRequest
            {
                ElectionResultIds =
                {
                        MajorityElectionResultMockedData
                            .IdStGallenElectionResultInContestBund,
                },
            });
        await RunEvents<MajorityElectionResultPlausibilised>();
    }

    protected override async Task SetAuditedTentatively()
    {
        await MonitoringElectionAdminClient
            .AuditedTentativelyAsync(new MajorityElectionResultAuditedTentativelyRequest
            {
                ElectionResultIds =
                {
                        MajorityElectionResultMockedData
                            .IdStGallenElectionResultInContestBund,
                },
            });
        await RunEvents<MajorityElectionResultAuditedTentatively>();
    }

    protected override async Task SetCorrectionDone()
    {
        await ErfassungElectionAdminClient
            .CorrectionFinishedAsync(new MajorityElectionResultCorrectionFinishedRequest
            {
                ElectionResultId = MajorityElectionResultMockedData
                    .IdStGallenElectionResultInContestBund,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });
        await RunEvents<MajorityElectionResultCorrectionFinished>();
    }

    protected override async Task SetReadyForCorrection()
    {
        await MonitoringElectionAdminClient
            .FlagForCorrectionAsync(new MajorityElectionResultFlagForCorrectionRequest
            {
                ElectionResultId = MajorityElectionResultMockedData
                    .IdStGallenElectionResultInContestBund,
            });
        await RunEvents<MajorityElectionResultFlaggedForCorrection>();
    }

    protected override async Task SetSubmissionDone()
    {
        await ErfassungElectionAdminClient
            .SubmissionFinishedAsync(new MajorityElectionResultSubmissionFinishedRequest
            {
                ElectionResultId = MajorityElectionResultMockedData.IdStGallenElectionResultInContestBund,
                SecondFactorTransactionId = SecondFactorTransactionMockedData.ExternalIdSecondFactorTransaction,
            });
        await RunEvents<MajorityElectionResultSubmissionFinished>();
    }

    protected override async Task SetSubmissionOngoing()
    {
        var contestGuid = Guid.Parse(ContestMockedData.IdBundesurnengang);
        var countingCircleGuid = CountingCircleMockedData.GuidStGallen;
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
                ContestId = ContestMockedData.IdBundesurnengang,
                CountingCircleId = CountingCircleMockedData.IdStGallen,
            });
        await RunEvents<MajorityElectionResultSubmissionStarted>();
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

    protected override GrpcChannel CreateGrpcChannel(params string[] roles)
        => CreateGrpcChannel(true, SecureConnectTestDefaults.MockedTenantStGallen.Id, TestDefaults.UserId, roles);

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
