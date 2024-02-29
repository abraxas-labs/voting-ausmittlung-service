// (c) Copyright 2024 by Abraxas Informatik AG
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

namespace Voting.Ausmittlung.Test.MockedData;

public static class MajorityElectionResultMockedData
{
    public static readonly Guid GuidStGallenElectionResultInContestBund = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
        CountingCircleMockedData.GuidStGallen,
        false);

    public static readonly Guid GuidStGallenElectionResultInContestStGallen = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen),
        CountingCircleMockedData.GuidStGallen,
        false);

    public static readonly Guid GuidUzwilElectionResultInContestUzwil = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds),
        CountingCircleMockedData.GuidUzwil,
        false);

    public static readonly Guid GuidKircheElectionResultInContestKirche = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche),
        CountingCircleMockedData.GuidUzwilKirche,
        false);

    public static readonly Guid GuidGossauElectionResultInContestGossau = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidGossauElectionResultInContestStGallen = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidUzwilElectionResultInContestStGallen = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
        CountingCircleMockedData.GuidUzwil,
        false);

    public static readonly Guid GuidStGallenElectionSecondaryResultInContestBund = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
        CountingCircleMockedData.GuidStGallen,
        false);

    public static readonly string IdStGallenElectionResultInContestBund = GuidStGallenElectionResultInContestBund.ToString();
    public static readonly string IdUzwilElectionResultInContestUzwil = GuidUzwilElectionResultInContestUzwil.ToString();
    public static readonly string IdKircheElectionResultInContestKirche = GuidKircheElectionResultInContestKirche.ToString();
    public static readonly string IdUzwilElectionResultInContestStGallen = GuidUzwilElectionResultInContestStGallen.ToString();

    public static MajorityElectionResult StGallenElectionResultInContestBund
        => new MajorityElectionResult
        {
            Id = GuidStGallenElectionResultInContestBund,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestBund),
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            TotalCountOfVoters = 15000,
            Entry = MajorityElectionResultEntry.Detailed,
            EntryParams = new MajorityElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 9000,
                ConventionalAccountedBallots = 8000,
                ConventionalBlankBallots = 500,
                ConventionalInvalidBallots = 500,
                VoterParticipation = 0.6m,
            },
            SecondaryMajorityElectionResults = new List<SecondaryMajorityElectionResult>
            {
                    new SecondaryMajorityElectionResult
                    {
                        Id = GuidStGallenElectionSecondaryResultInContestBund,
                        ConventionalSubTotal =
                        {
                            EmptyVoteCountExclWriteIns = 5,
                            IndividualVoteCount = 7,
                            InvalidVoteCount = 2,
                        },
                        SecondaryMajorityElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund),
                    },
                    new SecondaryMajorityElectionResult
                    {
                        Id = Guid.Parse("b4948d16-d6d5-49e4-908f-9c8147f02095"),
                        ConventionalSubTotal =
                        {
                            EmptyVoteCountExclWriteIns = 1,
                            IndividualVoteCount = 2,
                            InvalidVoteCount = 3,
                        },
                        SecondaryMajorityElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund2),
                    },
                    new SecondaryMajorityElectionResult
                    {
                        Id = Guid.Parse("1c9e37d7-7395-4251-bc00-50638d930e5a"),
                        ConventionalSubTotal =
                        {
                            EmptyVoteCountExclWriteIns = 99,
                            IndividualVoteCount = 100,
                            InvalidVoteCount = 101,
                        },
                        SecondaryMajorityElectionId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionIdStGallenMajorityElectionInContestBund3),
                    },
            },
        };

    public static MajorityElectionResult StGallenElectionResultInContestStGallen
        => new MajorityElectionResult
        {
            Id = GuidStGallenElectionResultInContestStGallen,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdStGallenMajorityElectionInContestStGallen),
            CountingCircleId = CountingCircleMockedData.GuidStGallen,
            TotalCountOfVoters = 9000,
            Entry = MajorityElectionResultEntry.Detailed,
            EntryParams = new MajorityElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 9000,
                ConventionalAccountedBallots = 8000,
                ConventionalBlankBallots = 500,
                ConventionalInvalidBallots = 500,
                VoterParticipation = 0.6m,
            },
        };

    public static MajorityElectionResult UzwilElectionResultInContestUzwil
        => new MajorityElectionResult
        {
            Id = GuidUzwilElectionResultInContestUzwil,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestUzwilWithoutChilds),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            TotalCountOfVoters = 20000,
            Entry = MajorityElectionResultEntry.FinalResults,
        };

    public static MajorityElectionResult KircheElectionResultInContestKirche
        => new MajorityElectionResult
        {
            Id = GuidKircheElectionResultInContestKirche,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdKircheMajorityElectionInContestKirche),
            CountingCircleId = CountingCircleMockedData.GuidUzwilKirche,
            TotalCountOfVoters = 50000,
            Entry = MajorityElectionResultEntry.Detailed,
            EntryParams = new MajorityElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
        };

    public static MajorityElectionResult GossauElectionResultInContestGossau
        => new MajorityElectionResult
        {
            Id = GuidGossauElectionResultInContestGossau,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestGossau),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 9000,
            Entry = MajorityElectionResultEntry.Detailed,
            EntryParams = new MajorityElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = false,
                BallotBundleSampleSize = 2,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 9000,
                ConventionalAccountedBallots = 8000,
                ConventionalBlankBallots = 500,
                ConventionalInvalidBallots = 500,
                VoterParticipation = 0.6m,
            },
        };

    public static MajorityElectionResult GossauElectionResultInContestStGallen
        => new MajorityElectionResult
        {
            Id = GuidGossauElectionResultInContestStGallen,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdGossauMajorityElectionInContestStGallen),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 8000,
            Entry = MajorityElectionResultEntry.Detailed,
            EntryParams = new MajorityElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 3,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 6000,
                ConventionalAccountedBallots = 5000,
                ConventionalBlankBallots = 500,
                ConventionalInvalidBallots = 500,
                VoterParticipation = 0.7m,
            },
        };

    public static MajorityElectionResult UzwilElectionResultInContestStGallen
        => new MajorityElectionResult
        {
            Id = GuidUzwilElectionResultInContestStGallen,
            MajorityElectionId = Guid.Parse(MajorityElectionMockedData.IdUzwilMajorityElectionInContestStGallen),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            TotalCountOfVoters = 8000,
            Entry = MajorityElectionResultEntry.Detailed,
            EntryParams = new MajorityElectionResultEntryParams
            {
                BallotBundleSize = 10,
                BallotNumberGeneration = BallotNumberGeneration.ContinuousForAllBundles,
                AutomaticEmptyVoteCounting = true,
                BallotBundleSampleSize = 3,
                AutomaticBallotBundleNumberGeneration = true,
                ReviewProcedure = MajorityElectionReviewProcedure.Electronically,
                CandidateCheckDigit = true,
            },
            CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 6000,
                ConventionalAccountedBallots = 5000,
                ConventionalBlankBallots = 500,
                ConventionalInvalidBallots = 500,
                VoterParticipation = 0.7m,
            },
        };

    public static IEnumerable<MajorityElectionResult> All
    {
        get
        {
            yield return StGallenElectionResultInContestBund;
            yield return StGallenElectionResultInContestStGallen;
            yield return UzwilElectionResultInContestUzwil;
            yield return KircheElectionResultInContestKirche;
            yield return GossauElectionResultInContestGossau;
            yield return GossauElectionResultInContestStGallen;
            yield return UzwilElectionResultInContestStGallen;
        }
    }

    public static IEnumerable<MajorityElectionResult> OnlyDetailed
    {
        get
        {
            yield return StGallenElectionResultInContestBund;
            yield return StGallenElectionResultInContestStGallen;
            yield return KircheElectionResultInContestKirche;
            yield return GossauElectionResultInContestGossau;
            yield return GossauElectionResultInContestStGallen;
            yield return UzwilElectionResultInContestStGallen;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, IEnumerable<MajorityElection> majorityElections, bool onlyDetailed)
    {
        await runScoped(async sp =>
        {
            var results = onlyDetailed ? OnlyDetailed.ToList() : All.ToList();
            var db = sp.GetRequiredService<DataContext>();

            foreach (var result in results)
            {
                var election = await db.MajorityElections.FindAsync(result.MajorityElectionId);
                var snapshotCountingCircle = await db.CountingCircles.FirstAsync(cc =>
                    cc.BasisCountingCircleId == result.CountingCircleId && cc.SnapshotContestId == election!.ContestId);
                result.CountingCircleId = snapshotCountingCircle.Id;

                if (result.Entry == MajorityElectionResultEntry.Detailed)
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
            }

            db.MajorityElectionResults.AddRange(results);
            await db.SaveChangesAsync();
        });

        await runScoped(async sp =>
        {
            // add not mocked results
            var merBuilder = sp.GetRequiredService<MajorityElectionResultBuilder>();
            foreach (var election in majorityElections)
            {
                await merBuilder.RebuildForElection(election.Id, election.DomainOfInfluenceId, ContestMockedData.TestingPhaseEnded(election.ContestId));
            }
        });
    }

    public static async Task InjectCandidateResults(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();

            var result = await db.MajorityElectionResults
                .AsTracking()
                .AsSplitQuery()
                .Include(x => x.CandidateResults)
                .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                .FirstAsync(x => x.Id == StGallenElectionResultInContestBund.Id);

            result.ConventionalSubTotal.IndividualVoteCount = 5;
            result.ConventionalSubTotal.EmptyVoteCountExclWriteIns = 6;
            result.ConventionalSubTotal.InvalidVoteCount = 7;
            result.EVotingSubTotal.IndividualVoteCount = 5;
            result.EVotingSubTotal.EmptyVoteCountExclWriteIns = 6;
            result.EVotingSubTotal.InvalidVoteCount = 7;
            result.EVotingSubTotal.EmptyVoteCountWriteIns = 2;
            result.EVotingSubTotal.EmptyVoteCountExclWriteIns = 3;
            result.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
            {
                ConventionalReceivedBallots = 100,
                ConventionalAccountedBallots = 80,
                ConventionalInvalidBallots = 10,
                ConventionalBlankBallots = 10,
                VoterParticipation = .8m,
            };

            var candidateId = Guid.Parse(MajorityElectionMockedData.CandidateId1StGallenMajorityElectionInContestBund);
            var candidateResult = result.CandidateResults.Single(x => x.CandidateId == candidateId);
            candidateResult.ConventionalVoteCount = 50;
            candidateResult.EVotingWriteInsVoteCount = 10;
            candidateResult.EVotingExclWriteInsVoteCount = 3;
            result.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = candidateResult.VoteCount;

            foreach (var secondaryResult in result.SecondaryMajorityElectionResults)
            {
                secondaryResult.ConventionalSubTotal.TotalCandidateVoteCountExclIndividual = 30;
                secondaryResult.EVotingSubTotal.TotalCandidateVoteCountExclIndividual = 7;
                secondaryResult.EVotingSubTotal.EmptyVoteCountWriteIns = 2;
                secondaryResult.EVotingSubTotal.EmptyVoteCountExclWriteIns = 1;
            }

            var secondaryCandidateId = Guid.Parse(MajorityElectionMockedData.SecondaryElectionCandidateId1StGallenMajorityElectionInContestBund);
            var secondaryCandidateResult = result.SecondaryMajorityElectionResults
                .SelectMany(x => x.CandidateResults)
                .First(x => x.CandidateId == secondaryCandidateId);
            secondaryCandidateResult.ConventionalVoteCount = 30;
            secondaryCandidateResult.EVotingWriteInsVoteCount = 3;
            secondaryCandidateResult.EVotingExclWriteInsVoteCount = 5;

            await db.SaveChangesAsync();
        });
    }
}
