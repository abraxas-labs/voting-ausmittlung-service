// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Test.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

public static class VoteEndResultMockedData
{
    public const string VoteId = "9008dfe3-c74a-4b7f-ae9a-e2a395b8208f";

    public const string BallotId1 = "970048ea-0eba-465e-ae29-049e388098f6";
    public const string BallotId2 = "490a26a4-bbb3-42e8-ba9c-82cbd3b4b87b";
    public const string BallotId3 = "95ded86a-a31a-40bf-b32a-bbc80ad8ea11";

    public const string Ballot1QuestionId = "66b3e98c-e7d5-4866-b456-30d3f91637f3";

    public const string Ballot2QuestionId1 = "d4ebf9fb-72c6-45a8-bc3b-0cba6f41d9a1";
    public const string Ballot2QuestionId2 = "5bee3f5a-e2d6-4de3-b537-271c01c2c9c4";
    public const string Ballot2TieBreakQuestionId = "d4b6a6ff-2759-4357-9adb-e809d8cfb08f";

    public const string Ballot3QuestionId1 = "cb945145-3ecf-4524-afdd-8bf3e93c5f0b";
    public const string Ballot3QuestionId2 = "ece450b2-820b-4ab7-bd88-c111daea2a98";
    public const string Ballot3QuestionId3 = "5c020918-7202-4551-b3a4-c9422687d78e";
    public const string Ballot3TieBreakQuestionId12 = "cfc129d2-ceb8-4a41-8e7c-3a1dc4f38a61";
    public const string Ballot3TieBreakQuestionId13 = "ad4a5fe3-432a-48b9-8e41-39fcdfeca0de";
    public const string Ballot3TieBreakQuestionId23 = "d7243e10-e582-4f9e-92dd-c5d99cf1c531";

    public static readonly Guid VoteGuid = Guid.Parse(VoteId);

    public static readonly Guid StGallenResultGuid =
        AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteGuid, CountingCircleMockedData.GuidStGallen, false);

    public static readonly Guid StGallenStFidenResultGuid =
        AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteGuid, CountingCircleMockedData.GuidStGallenStFiden, false);

    public static readonly Guid StGallenHaggenResultGuid =
        AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteGuid, CountingCircleMockedData.GuidStGallenHaggen, false);

    public static readonly Guid StGallenAuslandschweizerResultGuid =
        AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteGuid, CountingCircleMockedData.GuidStGallenAuslandschweizer, false);

    public static readonly Guid GossauResultGuid =
        AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteGuid, CountingCircleMockedData.GuidGossau, false);

    public static readonly Guid UzwilResultGuid =
        AusmittlungUuidV5.BuildPoliticalBusinessResult(VoteGuid, CountingCircleMockedData.GuidUzwil, false);

    public static readonly string StGallenResultId = StGallenResultGuid.ToString();
    public static readonly string StGallenStFidenResultId = StGallenStFidenResultGuid.ToString();
    public static readonly string StGallenHaggenResultId = StGallenHaggenResultGuid.ToString();
    public static readonly string StGallenAuslandschweizerResultId = StGallenAuslandschweizerResultGuid.ToString();
    public static readonly string GossauResultId = GossauResultGuid.ToString();
    public static readonly string UzwilResultId = UzwilResultGuid.ToString();

    public static Vote BuildVote(VoteResultAlgorithm resultAlgorithm)
    {
        return new Vote
        {
            Id = Guid.Parse(VoteId),
            PoliticalBusinessNumber = "201",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Abst SG"),
            InternalDescription = "Abstimmung St. Gallen auf Urnengang Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ReportDomainOfInfluenceLevel = 1,
            Active = true,
            ResultAlgorithm = resultAlgorithm,
            Ballots = new List<Ballot>
                {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotId1),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(Ballot1QuestionId),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1"),
                                Type = BallotQuestionType.MainBallot,
                                FederalIdentification = 555,
                            },
                        },
                    },
                    new Ballot
                    {
                        Id = Guid.Parse(BallotId2),
                        Position = 2,
                        BallotType = BallotType.VariantsBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(Ballot2QuestionId1),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1"),
                                Type = BallotQuestionType.MainBallot,
                                FederalIdentification = 666,
                            },
                            new BallotQuestion
                            {
                                Number = 2,
                                Id = Guid.Parse(Ballot2QuestionId2),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 2"),
                                Type = BallotQuestionType.CounterProposal,
                                FederalIdentification = 777,
                            },
                        },
                        HasTieBreakQuestions = true,
                        TieBreakQuestions = new List<TieBreakQuestion>
                        {
                            new TieBreakQuestion
                            {
                                Number = 1,
                                Question1Number = 1,
                                Question2Number = 2,
                                Id = Guid.Parse(Ballot2TieBreakQuestionId),
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Welcher der beiden Vorlagen soll in Kraft treten, falls sowohl der Beschluss als auch der Gegenvorschlag angenommen werden?"),
                                FederalIdentification = 888,
                            },
                        },
                    },
                    new Ballot
                    {
                        Id = Guid.Parse(BallotId3),
                        Position = 3,
                        BallotType = BallotType.VariantsBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(Ballot3QuestionId1),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1"),
                                Type = BallotQuestionType.MainBallot,
                            },
                            new BallotQuestion
                            {
                                Number = 2,
                                Id = Guid.Parse(Ballot3QuestionId2),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 2"),
                                Type = BallotQuestionType.CounterProposal,
                            },
                            new BallotQuestion
                            {
                                Number = 3,
                                Id = Guid.Parse(Ballot3QuestionId3),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 3"),
                                Type = BallotQuestionType.CounterProposal,
                            },
                        },
                        HasTieBreakQuestions = true,
                        TieBreakQuestions = new List<TieBreakQuestion>
                        {
                            new TieBreakQuestion
                            {
                                Number = 1,
                                Question1Number = 1,
                                Question2Number = 2,
                                Id = Guid.Parse(Ballot3TieBreakQuestionId12),
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "1 vor 2?"),
                            },
                            new TieBreakQuestion
                            {
                                Number = 2,
                                Question1Number = 1,
                                Question2Number = 3,
                                Id = Guid.Parse(Ballot3TieBreakQuestionId13),
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "1 vor 3?"),
                            },
                            new TieBreakQuestion
                            {
                                Number = 3,
                                Question1Number = 2,
                                Question2Number = 3,
                                Id = Guid.Parse(Ballot3TieBreakQuestionId23),
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "2 vor 3?"),
                            },
                        },
                    },
                },
        };
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        var vote = BuildVote(VoteResultAlgorithm.PopularMajority);

        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();
            var simplePbBuilder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<Vote>>();

            var mappedDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                doi.SnapshotContestId == vote.ContestId && doi.BasisDomainOfInfluenceId == vote.DomainOfInfluenceId);
            vote.DomainOfInfluenceId = mappedDomainOfInfluence.Id;

            db.Votes.Add(vote);
            await db.SaveChangesAsync();

            await simplePbBuilder.Create(vote);

            await sp.GetRequiredService<VoteResultBuilder>()
                .RebuildForVote(vote.Id, mappedDomainOfInfluence.Id, false, vote.ContestId);

            var endResultInitializer = sp.GetRequiredService<VoteEndResultInitializer>();
            await endResultInitializer.RebuildForVote(vote.Id, false);

            var results = await db.VoteResults
                .AsTracking()
                .Where(x => x.VoteId == vote.Id)
                .Include(x => x.Results)
                .ToListAsync();

            SetResultsMockData(results);
            await db.SaveChangesAsync();

            var endResultBuilder = sp.GetRequiredService<VoteEndResultBuilder>();
            foreach (var result in results)
            {
                await endResultBuilder.AdjustVoteEndResult(result.Id, false);
            }

            await db.SaveChangesAsync();
        });
    }

    private static void SetResultsMockData(IEnumerable<VoteResult> results)
    {
        foreach (var result in results)
        {
            SetResultMockData(result);
        }
    }

    private static void SetResultMockData(VoteResult result)
    {
        result.State = CountingCircleResultState.SubmissionDone;
        result.TotalCountOfVoters = 1000;

        var ballotResults = result.Results.ToDictionary(x => x.BallotId);
        var ballotResult1 = ballotResults[Guid.Parse(BallotId1)];
        var ballotResult2 = ballotResults[Guid.Parse(BallotId2)];
        var ballotResult3 = ballotResults[Guid.Parse(BallotId3)];

        ballotResult1.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
        {
            ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
            {
                ReceivedBallots = 500,
                InvalidBallots = 200,
                BlankBallots = 80,
                AccountedBallots = 220,
            },
            EVotingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 330,
                InvalidBallots = 25,
                BlankBallots = 17,
                AccountedBallots = 288,
            },
            ECountingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 333,
                InvalidBallots = 26,
                BlankBallots = 18,
                AccountedBallots = 289,
            },
            VoterParticipation = .5m,
        };

        ballotResult2.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
        {
            ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
            {
                ReceivedBallots = 450,
                InvalidBallots = 180,
                BlankBallots = 80,
                AccountedBallots = 200,
            },
            EVotingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 315,
                InvalidBallots = 20,
                BlankBallots = 12,
                AccountedBallots = 283,
            },
            ECountingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 318,
                InvalidBallots = 21,
                BlankBallots = 13,
                AccountedBallots = 284,
            },
            VoterParticipation = .5m,
        };

        ballotResult3.CountOfVoters = new PoliticalBusinessNullableCountOfVoters
        {
            ConventionalSubTotal = new PoliticalBusinessCountOfVotersNullableSubTotal
            {
                ReceivedBallots = 400,
                InvalidBallots = 20,
                BlankBallots = 10,
                AccountedBallots = 370,
            },
            EVotingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 300,
                InvalidBallots = 15,
                BlankBallots = 7,
                AccountedBallots = 278,
            },
            ECountingSubTotal = new PoliticalBusinessCountOfVotersSubTotal
            {
                ReceivedBallots = 303,
                InvalidBallots = 16,
                BlankBallots = 8,
                AccountedBallots = 279,
            },
            VoterParticipation = .45m,
        };

        var questionAnswerCountsByQuestionId = new Dictionary<string, (int, int, int, int, int, int, int, int, int)>
            {
                { Ballot1QuestionId, (190, 95, 19, 10, 5, 1, 11, 6, 2) },
                { Ballot2QuestionId1, (38, 90, 20, 2, 10, 3, 3, 11, 4) },
                { Ballot2QuestionId2, (75, 50, 13, 25, 0, 0, 26, 0, 0) },
                { Ballot2TieBreakQuestionId, (20, 27, 113, 0, 3, 0, 0, 4, 0) },
                { Ballot3QuestionId1, (10, 200, 170, 0, 0, 0, 0, 0, 0) },
                { Ballot3QuestionId2, (5, 205, 150, 15, 5, 0, 16, 6, 0) },
                { Ballot3QuestionId3, (80, 90, 190, 0, 0, 10, 0, 0, 11) },
                { Ballot3TieBreakQuestionId12, (100, 150, 120, 0, 0, 0, 0, 0, 0) },
                { Ballot3TieBreakQuestionId13, (160, 100, 105, 0, 0, 5, 0, 0, 6) },
                { Ballot3TieBreakQuestionId23, (67, 210, 90, 13, 0, 0, 14, 0, 0) },
            };

        foreach (var questionAnswerCountsKvp in questionAnswerCountsByQuestionId)
        {
            var questionId = Guid.Parse(questionAnswerCountsKvp.Key);

            var questionResult = ballotResult1.QuestionResults.FirstOrDefault(x => x.QuestionId == questionId)
                ?? ballotResult2.QuestionResults.FirstOrDefault(x => x.QuestionId == questionId)
                ?? ballotResult3.QuestionResults.FirstOrDefault(x => x.QuestionId == questionId);

            if (questionResult == null)
            {
                continue;
            }

            questionResult.ConventionalSubTotal.TotalCountOfAnswerYes = questionAnswerCountsKvp.Value.Item1;
            questionResult.ConventionalSubTotal.TotalCountOfAnswerNo = questionAnswerCountsKvp.Value.Item2;
            questionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified = questionAnswerCountsKvp.Value.Item3;
            questionResult.EVotingSubTotal.TotalCountOfAnswerYes = questionAnswerCountsKvp.Value.Item4;
            questionResult.EVotingSubTotal.TotalCountOfAnswerNo = questionAnswerCountsKvp.Value.Item5;
            questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified = questionAnswerCountsKvp.Value.Item6;
            questionResult.ECountingSubTotal.TotalCountOfAnswerYes = questionAnswerCountsKvp.Value.Item7;
            questionResult.ECountingSubTotal.TotalCountOfAnswerNo = questionAnswerCountsKvp.Value.Item8;
            questionResult.ECountingSubTotal.TotalCountOfAnswerUnspecified = questionAnswerCountsKvp.Value.Item9;
        }

        foreach (var questionAnswerCountsKvp in questionAnswerCountsByQuestionId)
        {
            var questionId = Guid.Parse(questionAnswerCountsKvp.Key);

            var questionResult = ballotResult1.TieBreakQuestionResults.FirstOrDefault(x => x.QuestionId == questionId)
                                 ?? ballotResult2.TieBreakQuestionResults.FirstOrDefault(x => x.QuestionId == questionId)
                                 ?? ballotResult3.TieBreakQuestionResults.FirstOrDefault(x => x.QuestionId == questionId);

            if (questionResult == null)
            {
                continue;
            }

            questionResult.ConventionalSubTotal.TotalCountOfAnswerQ1 = questionAnswerCountsKvp.Value.Item1;
            questionResult.ConventionalSubTotal.TotalCountOfAnswerQ2 = questionAnswerCountsKvp.Value.Item2;
            questionResult.ConventionalSubTotal.TotalCountOfAnswerUnspecified = questionAnswerCountsKvp.Value.Item3;
            questionResult.EVotingSubTotal.TotalCountOfAnswerQ1 = questionAnswerCountsKvp.Value.Item4;
            questionResult.EVotingSubTotal.TotalCountOfAnswerQ2 = questionAnswerCountsKvp.Value.Item5;
            questionResult.EVotingSubTotal.TotalCountOfAnswerUnspecified = questionAnswerCountsKvp.Value.Item6;
            questionResult.ECountingSubTotal.TotalCountOfAnswerQ1 = questionAnswerCountsKvp.Value.Item5;
            questionResult.ECountingSubTotal.TotalCountOfAnswerQ2 = questionAnswerCountsKvp.Value.Item6;
            questionResult.ECountingSubTotal.TotalCountOfAnswerUnspecified = questionAnswerCountsKvp.Value.Item7;
        }
    }
}
