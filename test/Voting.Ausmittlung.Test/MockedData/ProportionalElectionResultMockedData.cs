// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Testing.Mocks;

namespace Voting.Ausmittlung.Test.MockedData;

public static class ProportionalElectionResultMockedData
{
    public static readonly Guid GuidGossauElectionResultInContestStGallen = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidGossauElectionResultInContestBund = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidUzwilElectionResultInContestUzwil = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds),
        CountingCircleMockedData.GuidUzwil,
        false);

    public static readonly Guid GuidGossauElectionResultInContestGossau = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly string IdGossauElectionResultInContestStGallen = GuidGossauElectionResultInContestStGallen.ToString();
    public static readonly string IdUzwilElectionResultInContestUzwil = GuidUzwilElectionResultInContestUzwil.ToString();

    public static ProportionalElectionResult GossauElectionResultInContestStGallen
        => new ProportionalElectionResult
        {
            Id = GuidGossauElectionResultInContestStGallen,
            ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestStGallen),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 15000,
            EntryParams = new ProportionalElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            },
        };

    public static ProportionalElectionResult StGallenElectionResultGossauInContestBund
        => new ProportionalElectionResult
        {
            Id = GuidGossauElectionResultInContestBund,
            ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdStGallenProportionalElectionInContestBund),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 15000,
            EntryParams = new ProportionalElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            },
        };

    public static ProportionalElectionResult UzwilElectionResultInContestUzwil
        => new ProportionalElectionResult
        {
            Id = GuidUzwilElectionResultInContestUzwil,
            ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdUzwilProportionalElectionInContestUzwilWithoutChilds),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            TotalCountOfVoters = 20000,
            AuditedTentativelyTimestamp = MockedClock.GetDate(hoursDelta: -3),
            SubmissionDoneTimestamp = MockedClock.GetDate(hoursDelta: -12),
        };

    public static ProportionalElectionResult GossauElectionResultInContestGossau
        => new ProportionalElectionResult
        {
            Id = GuidGossauElectionResultInContestGossau,
            ProportionalElectionId = Guid.Parse(ProportionalElectionMockedData.IdGossauProportionalElectionInContestGossau),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 6000,
            EntryParams = new ProportionalElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = ProportionalElectionReviewProcedure.Electronically,
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 5000,
                ConventionalBlankBallots = 20,
                ConventionalInvalidBallots = 80,
                ConventionalAccountedBallots = 4900,
                VoterParticipation = 0.8m,
            },
        };

    public static IEnumerable<ProportionalElectionResult> All
    {
        get
        {
            yield return GossauElectionResultInContestStGallen;
            yield return StGallenElectionResultGossauInContestBund;
            yield return UzwilElectionResultInContestUzwil;
            yield return GossauElectionResultInContestGossau;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, IEnumerable<ProportionalElection> proportionalElections)
    {
        await runScoped(async sp =>
        {
            var results = All.ToList();
            var db = sp.GetRequiredService<DataContext>();

            foreach (var result in results)
            {
                var election = await db.ProportionalElections.FindAsync(result.ProportionalElectionId);
                var snapshotCountingCircle = await db.CountingCircles.FirstAsync(cc =>
                    cc.BasisCountingCircleId == result.CountingCircleId && cc.SnapshotContestId == election!.ContestId);
                result.CountingCircleId = snapshotCountingCircle.Id;
            }

            db.ProportionalElectionResults.AddRange(results);
            await db.SaveChangesAsync();

            // add not mocked results
            var perRepo = sp.GetRequiredService<ProportionalElectionResultBuilder>();
            foreach (var election in proportionalElections)
            {
                await perRepo.RebuildForElection(election.Id, election.DomainOfInfluenceId, ContestMockedData.TestingPhaseEnded(election.ContestId));
            }
        });
    }
}
