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

public static class VoteResultMockedData
{
    public static readonly Guid GuidBundVote2InContestBundResult = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(VoteMockedData.IdBundVote2InContestBund),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidGossauVoteInContestGossauResult = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidGossauVoteInContestStGallenResult = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
        CountingCircleMockedData.GuidGossau,
        false);

    public static readonly Guid GuidUzwilVoteInContestStGallenResult = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen),
        CountingCircleMockedData.GuidUzwil,
        false);

    public static readonly Guid GuidUzwilVoteInContestUzwilResult = AusmittlungUuidV5.BuildPoliticalBusinessResult(
        Guid.Parse(VoteMockedData.IdUzwilVoteInContestUzwilWithoutChilds),
        CountingCircleMockedData.GuidUzwil,
        false);

    public static readonly string IdGossauVoteInContestGossauResult = GuidGossauVoteInContestGossauResult.ToString();
    public static readonly string IdGossauVoteInContestStGallenResult = GuidGossauVoteInContestStGallenResult.ToString();
    public static readonly string IdUzwilVoteInContestStGallenResult = GuidUzwilVoteInContestStGallenResult.ToString();
    public static readonly string IdUzwilVoteInContestUzwilResult = GuidUzwilVoteInContestUzwilResult.ToString();

    public static readonly Guid GuidGossauVoteInContestStGallenBallotResult = AusmittlungUuidV5.BuildVoteBallotResult(
        Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestStGallen),
        CountingCircleMockedData.GuidGossau);

    public static readonly string IdGossauVoteInContestStGallenBallotResult =
        GuidGossauVoteInContestStGallenBallotResult.ToString();

    public static readonly Guid GuidUzwilVoteInContestStGallenBallotResult = AusmittlungUuidV5.BuildVoteBallotResult(
        Guid.Parse(VoteMockedData.BallotIdUzwilVoteInContestStGallen),
        CountingCircleMockedData.GuidUzwil);

    public static readonly string IdUzwilVoteInContestStGallenBallotResult = GuidUzwilVoteInContestStGallenBallotResult.ToString();

    public static VoteResult GossauBundVote2InContestBundResult
        => new VoteResult
        {
            Id = GuidBundVote2InContestBundResult,
            VoteId = Guid.Parse(VoteMockedData.IdBundVote2InContestBund),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 40_000,
            SubmissionDoneTimestamp = new DateTime(2020, 10, 2, 14, 10, 6, DateTimeKind.Utc),
            Entry = VoteResultEntry.Detailed,
            EntryParams = new VoteResultEntryParams
            {
                AutomaticBallotBundleNumberGeneration = true,
                BallotBundleSampleSizePercent = 15,
                ReviewProcedure = VoteReviewProcedure.Electronically,
            },
            Results =
            {
                    new BallotResult
                    {
                        Id = AusmittlungUuidV5.BuildVoteBallotResult(
                            Guid.Parse(VoteMockedData.BallotId1BundVote2InContestBund),
                            CountingCircleMockedData.GuidGossau),
                        BallotId = Guid.Parse(VoteMockedData.BallotId1BundVote2InContestBund),
                        CountOfVoters = new PoliticalBusinessNullableCountOfVoters
                        {
                            ConventionalReceivedBallots = 10000,
                            ConventionalInvalidBallots = 3000,
                            ConventionalAccountedBallots = 2000,
                            ConventionalBlankBallots = 2000,
                            VoterParticipation = .5m,
                        },
                        QuestionResults =
                        {
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("96bb5101-d38b-4dbe-a669-b8eaa81f0a1c"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestion11IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 4500,
                                    TotalCountOfAnswerNo = 3950,
                                    TotalCountOfAnswerUnspecified = 60,
                                },
                                EVotingSubTotal =
                                {
                                    TotalCountOfAnswerYes = 1000,
                                    TotalCountOfAnswerNo = 450,
                                    TotalCountOfAnswerUnspecified = 40,
                                },
                            },
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("5d0f3e9c-fef6-4058-9476-ea654c38dcb4"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestion12IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 5502,
                                    TotalCountOfAnswerNo = 4402,
                                    TotalCountOfAnswerUnspecified = 102,
                                },
                            },
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("62cdd166-3fc3-4d9e-9a4c-a78c349b0d68"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestion13IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 4502,
                                    TotalCountOfAnswerNo = 3952,
                                    TotalCountOfAnswerUnspecified = 62,
                                },
                                EVotingSubTotal =
                                {
                                    TotalCountOfAnswerYes = 1003,
                                    TotalCountOfAnswerNo = 453,
                                    TotalCountOfAnswerUnspecified = 43,
                                },
                            },
                        },
                        TieBreakQuestionResults = new List<TieBreakQuestionResult>
                        {
                            new TieBreakQuestionResult
                            {
                                Id = Guid.Parse("ce020903-4bda-42d0-ad15-eaf3bf468bcd"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotTieBreakQuestion11IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerQ1 = 100,
                                    TotalCountOfAnswerQ2 = 200,
                                    TotalCountOfAnswerUnspecified = 20,
                                },
                            },
                            new TieBreakQuestionResult
                            {
                                Id = Guid.Parse("7d501cbd-fbfb-4a93-887f-df0863c9aede"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotTieBreakQuestion12IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerQ1 = 101,
                                    TotalCountOfAnswerQ2 = 201,
                                    TotalCountOfAnswerUnspecified = 21,
                                },
                            },
                            new TieBreakQuestionResult
                            {
                                Id = Guid.Parse("6e186da0-17c2-4855-96be-9d10cf6fb344"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotTieBreakQuestion13IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerQ1 = 102,
                                    TotalCountOfAnswerQ2 = 202,
                                    TotalCountOfAnswerUnspecified = 22,
                                },
                            },
                        },
                    },
                    new BallotResult
                    {
                        Id = AusmittlungUuidV5.BuildVoteBallotResult(
                            Guid.Parse(VoteMockedData.BallotId2BundVote2InContestBund),
                            CountingCircleMockedData.GuidGossau),
                        BallotId = Guid.Parse(VoteMockedData.BallotId2BundVote2InContestBund),
                        CountOfVoters = new PoliticalBusinessNullableCountOfVoters
                        {
                            ConventionalReceivedBallots = 3194,
                            ConventionalInvalidBallots = 500,
                            ConventionalAccountedBallots = 1694,
                            ConventionalBlankBallots = 1000,
                            VoterParticipation = 3194m / 40_000,
                        },
                        QuestionResults =
                        {
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("b097df55-882b-4733-b404-10e1ba0657cd"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestion21IdBundVote2InContestBund),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 123,
                                    TotalCountOfAnswerNo = 456,
                                    TotalCountOfAnswerUnspecified = 5,
                                },
                                EVotingSubTotal =
                                {
                                    TotalCountOfAnswerYes = 500,
                                    TotalCountOfAnswerNo = 600,
                                    TotalCountOfAnswerUnspecified = 10,
                                },
                            },
                        },
                    },
            },
        };

    public static VoteResult GossauVoteInContestGossauResult
        => new VoteResult
        {
            Id = GuidGossauVoteInContestGossauResult,
            VoteId = Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 20000,
            Entry = VoteResultEntry.FinalResults,
            Results =
            {
                    new BallotResult
                    {
                        Id = AusmittlungUuidV5.BuildVoteBallotResult(
                            Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestGossau),
                            CountingCircleMockedData.GuidGossau),
                        BallotId = Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestGossau),
                        CountOfVoters = new PoliticalBusinessNullableCountOfVoters
                        {
                            ConventionalReceivedBallots = 10000,
                            ConventionalInvalidBallots = 3000,
                            ConventionalAccountedBallots = 2000,
                            ConventionalBlankBallots = 2000,
                            VoterParticipation = .5m,
                        },
                        QuestionResults =
                        {
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("e319000d-0143-40b8-9db9-220a321268db"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestionIdGossauVoteInContestGossau),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 5500,
                                    TotalCountOfAnswerNo = 3950,
                                    TotalCountOfAnswerUnspecified = 60,
                                },
                                EVotingSubTotal =
                                {
                                    TotalCountOfAnswerYes = 1000,
                                    TotalCountOfAnswerNo = 450,
                                    TotalCountOfAnswerUnspecified = 40,
                                },
                            },
                        },
                    },
            },
        };

    public static VoteResult GossauVoteInContestStGallenResult
        => new VoteResult
        {
            Id = GuidGossauVoteInContestStGallenResult,
            VoteId = Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen),
            CountingCircleId = CountingCircleMockedData.GuidGossau,
            TotalCountOfVoters = 15000,
            Entry = VoteResultEntry.Detailed,
            EntryParams = new VoteResultEntryParams
            {
                AutomaticBallotBundleNumberGeneration = false,
                BallotBundleSampleSizePercent = 30,
                ReviewProcedure = VoteReviewProcedure.Electronically,
            },
            Results =
            {
                    new BallotResult
                    {
                        Id = Guid.Parse(IdGossauVoteInContestStGallenBallotResult),
                        BallotId = Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestStGallen),
                        CountOfVoters = new PoliticalBusinessNullableCountOfVoters
                        {
                            ConventionalReceivedBallots = 20000,
                            ConventionalInvalidBallots = 3000,
                            ConventionalAccountedBallots = 12000,
                            ConventionalBlankBallots = 2000,
                            VoterParticipation = .75m,
                        },
                        QuestionResults =
                        {
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("2058ba46-58de-4dab-ab04-b6a203e7d621"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestion1IdGossauVoteInContestStGallen),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 5500,
                                    TotalCountOfAnswerNo = 4490,
                                    TotalCountOfAnswerUnspecified = 10,
                                },
                            },
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("092033c1-d3b3-4c12-9c0b-35b1d35f555e"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestion2IdGossauVoteInContestStGallen),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 4500,
                                    TotalCountOfAnswerNo = 6500,
                                },
                            },
                        },
                        TieBreakQuestionResults =
                        {
                            new TieBreakQuestionResult
                            {
                                Id = Guid.Parse("0ee2274e-3a63-4e08-9fe1-2c68146d2e80"),
                                QuestionId = Guid.Parse(VoteMockedData.TieBreakQuestionIdGossauVoteInContestStGallen),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerQ1 = 4500,
                                    TotalCountOfAnswerQ2 = 6400,
                                    TotalCountOfAnswerUnspecified = 100,
                                },
                            },
                        },
                    },
            },
        };

    public static VoteResult UzwilVoteInContestStGallenResult
        => new VoteResult
        {
            Id = GuidUzwilVoteInContestStGallenResult,
            VoteId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestStGallen),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            TotalCountOfVoters = 15000,
            Entry = VoteResultEntry.Detailed,
            EntryParams = new VoteResultEntryParams
            {
                AutomaticBallotBundleNumberGeneration = true,
                BallotBundleSampleSizePercent = 50,
                ReviewProcedure = VoteReviewProcedure.Electronically,
            },
            Results =
            {
                    new BallotResult
                    {
                        Id = Guid.Parse(IdUzwilVoteInContestStGallenBallotResult),
                        BallotId = Guid.Parse(VoteMockedData.BallotIdUzwilVoteInContestStGallen),
                        CountOfVoters = new PoliticalBusinessNullableCountOfVoters
                        {
                            ConventionalReceivedBallots = 20000,
                            ConventionalInvalidBallots = 3000,
                            ConventionalAccountedBallots = 12000,
                            ConventionalBlankBallots = 2000,
                            VoterParticipation = .75m,
                        },
                        QuestionResults =
                        {
                            new BallotQuestionResult
                            {
                                Id = Guid.Parse("f0998998-8f99-4350-834e-f83901e79608"),
                                QuestionId = Guid.Parse(VoteMockedData.BallotQuestionIdUzwilVoteInContestStGallen),
                                ConventionalSubTotal =
                                {
                                    TotalCountOfAnswerYes = 5300,
                                    TotalCountOfAnswerNo = 4500,
                                    TotalCountOfAnswerUnspecified = 200,
                                },
                            },
                        },
                    },
            },
        };

    public static VoteResult UzwilVoteInContestUzwilResult
        => new VoteResult
        {
            Id = GuidUzwilVoteInContestUzwilResult,
            VoteId = Guid.Parse(VoteMockedData.IdUzwilVoteInContestUzwilWithoutChilds),
            CountingCircleId = CountingCircleMockedData.GuidUzwil,
            TotalCountOfVoters = 5000,
        };

    public static IEnumerable<VoteResult> All
    {
        get
        {
            yield return GossauBundVote2InContestBundResult;
            yield return GossauVoteInContestGossauResult;
            yield return GossauVoteInContestStGallenResult;
            yield return UzwilVoteInContestStGallenResult;
            yield return UzwilVoteInContestUzwilResult;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped, IEnumerable<Vote> votes)
    {
        await runScoped(async sp =>
        {
            var voteResults = All.ToList();
            var db = sp.GetRequiredService<DataContext>();

            foreach (var voteResult in voteResults)
            {
                var vote = await db.Votes.FindAsync(voteResult.VoteId);
                var snapshotCountingCircle = await db.CountingCircles.FirstAsync(cc =>
                    cc.BasisCountingCircleId == voteResult.CountingCircleId && cc.SnapshotContestId == vote!.ContestId);
                voteResult.CountingCircleId = snapshotCountingCircle.Id;
            }

            db.VoteResults.AddRange(voteResults);
            await db.SaveChangesAsync();

            // auto init not mocked vote results
            var voteResultBuilder = sp.GetRequiredService<VoteResultBuilder>();
            foreach (var vote in votes)
            {
                await voteResultBuilder.RebuildForVote(vote.Id, vote.DomainOfInfluenceId, ContestMockedData.TestingPhaseEnded(vote.ContestId));
            }
        });
    }
}
