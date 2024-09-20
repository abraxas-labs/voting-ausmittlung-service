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
using Voting.Ausmittlung.Test.Utils;

namespace Voting.Ausmittlung.Test.MockedData;

public static class VoteMockedData
{
    public const string IdBundVoteInContestArchivedBund = "36b12d9d-da6c-4ada-8115-b6c09af92a01";
    public const string IdBundVoteInContestVergangenBund = "3237ad60-b8da-4977-93c2-fa3219bf22f3";
    public const string IdBundVoteInContestBund = "7550e7c2-27e3-4602-a928-6f9c2afd2289";
    public const string IdBundVote2InContestBund = "0ec1d911-3157-452b-9056-f98b6c481d6f";
    public const string IdStGallenVoteInContestBund = "903a47bf-6b7b-4460-8475-b7bd89ea2ac9";
    public const string IdUzwilVoteInContestBundWithoutChilds = "6b4c7f11-9860-4468-ace9-78baec913a8d";
    public const string IdBundVoteInContestStGallen = "f69b3543-ccee-467d-9cde-56941f6e4bad";
    public const string IdGossauVoteInContestStGallen = "8076dee2-f19b-4af9-80b1-69b0c7b1402b";
    public const string IdUzwilVoteInContestStGallen = "da65e354-f668-4ae4-b3ef-c1a74764e99d";
    public const string IdStGallenVoteInContestStGallen = "96d8275f-f1f8-4933-a097-5c0c19f54567";
    public const string IdStGallenVoteInContestStGallenWithoutChilds = "607a9dbc-250a-4bbf-ab31-73c81f6556ba";
    public const string IdGossauVoteInContestGossau = "8fbba43a-cd73-407a-b490-df13c41cc5ee";
    public const string IdUzwilVoteInContestUzwilWithoutChilds = "7de846be-9e60-45c7-81e3-51441ff37592";
    public const string IdGenfVoteInContestBundWithoutChilds = "b751e349-0d2c-482c-b5f9-780608cca9f8";
    public const string IdKircheVoteInContestKircheWithoutChilds = "bfd3a5ba-a9e5-4cdd-9b81-16a181cf53cb";

    public const string BallotIdBundVoteInContestArchivedBund = "258207b2-df3c-425b-b96d-8aabfbfbeede";
    public const string BallotIdBundVoteInContestVergangenBund = "901a9563-fe96-4554-ba60-3600bb0b4794";
    public const string BallotIdBundVoteInContestBund = "b7ba9fef-27f9-46c4-8046-955e874561a7";
    public const string BallotId1BundVote2InContestBund = "be5c19a2-f51c-42e4-bda9-1e3cdeb3d5d2";
    public const string BallotId2BundVote2InContestBund = "f27ceb14-c255-44be-84ce-0b82047f230d";
    public const string BallotIdStGallenVoteInContestBund = "60dd6c2c-e73a-467e-99e1-902f973a5d8e";
    public const string BallotIdBundVoteInContestStGallen = "512bb3a2-97e2-4779-ac51-83abd039afc4";
    public const string BallotIdGossauVoteInContestStGallen = "154ee710-88b7-419a-ae5b-74a44c9c969e";
    public const string BallotIdUzwilVoteInContestStGallen = "e6ee82f9-70d4-4ffa-a673-34d56fc47204";
    public const string BallotIdStGallenVoteInContestStGallen = "0ed26aad-e169-4d55-b9a9-90475ba81a02";
    public const string BallotIdGossauVoteInContestGossau = "a2aa6f61-752b-446f-94b4-002a95111c7a";
    public const string BallotIdUzwilVoteInContestUzwil = "7ad05ec9-0ad9-4490-8b97-368f7175a7f0";

    public const string BallotQuestion11IdBundVote2InContestBund = "6db22eb7-2874-49e6-a6f1-389879d75433";
    public const string BallotQuestion12IdBundVote2InContestBund = "8a8d3b13-86f7-4ccb-8cad-581c2605bad1";
    public const string BallotQuestion13IdBundVote2InContestBund = "dd4b9b69-f8cd-4a8d-90ff-8fabdd3c1aca";
    public const string BallotQuestion21IdBundVote2InContestBund = "c08eae90-c0fb-4deb-a52a-9e16fcb46c55";
    public const string BallotTieBreakQuestion11IdBundVote2InContestBund = "eb54f06d-c856-43ac-afeb-10c32f7246eb";
    public const string BallotTieBreakQuestion12IdBundVote2InContestBund = "02a57628-2750-4e81-9316-674aefe3c1d3";
    public const string BallotTieBreakQuestion13IdBundVote2InContestBund = "d0f99d5a-59cb-4063-9334-866ad6011b0f";

    public const string BallotQuestionIdGossauVoteInContestGossau = "f1344734-0ac3-4c1b-aafe-c39bfb2f277b";
    public const string BallotQuestionIdUzwilVoteInContestStGallen = "191761e9-c08a-4d36-b693-cd9ec56f73ca";

    public const string BallotQuestion1IdGossauVoteInContestStGallen = "0516dfaf-4414-4f71-b062-d39dc4bb0277";
    public const string BallotQuestion2IdGossauVoteInContestStGallen = "e4dc0d77-5fb7-45f0-9c54-10d2c1d1660d";
    public const string TieBreakQuestionIdGossauVoteInContestStGallen = "7c2e10f6-1262-45d9-a705-b480aa035d0e";

    public static Vote BundVoteInContestArchivedBund
        => new Vote
        {
            Id = Guid.Parse(IdBundVoteInContestArchivedBund),
            PoliticalBusinessNumber = "200",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Bund",
                (t, s) => t.ShortDescription = s,
                "Abst Bund"),
            InternalDescription = "Abstimmung Bund auf Urnengang Bund Archived",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdArchivedBundesurnengang),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdBundVoteInContestArchivedBund),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("ac6f3e33-3c3e-445e-8184-7d0983fa0986"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote BundVoteInContestVergangenBund
        => new Vote
        {
            Id = Guid.Parse(IdBundVoteInContestVergangenBund),
            PoliticalBusinessNumber = "200",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Bund",
                (t, s) => t.ShortDescription = s,
                "Abst Bund"),
            InternalDescription = "Abstimmung Bund auf Urnengang vergangen Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdVergangenerBundesurnengang),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdBundVoteInContestVergangenBund),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("ac84c4a3-14cb-4958-8630-6976cf66549a"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote BundVoteInContestBund
        => new Vote
        {
            Id = Guid.Parse(IdBundVoteInContestBund),
            PoliticalBusinessNumber = "200",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Bund",
                (t, s) => t.ShortDescription = s,
                "Abst Bund"),
            InternalDescription = "Abstimmung Bund auf Urnengang Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdBundVoteInContestBund),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("21507485-971a-4a1d-b1ac-5b153d0e3082"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote BundVote2InContestBund
        => new Vote
        {
            Id = Guid.Parse(IdBundVote2InContestBund),
            PoliticalBusinessNumber = "202",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung2 Bund",
                (t, s) => t.ShortDescription = s,
                "Abst2 Bund"),
            InternalDescription = "Abstimmung2 Bund auf Urnengang Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            BallotBundleSampleSizePercent = 10,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotId1BundVote2InContestBund),
                        Position = 1,
                        BallotType = BallotType.VariantsBallot,
                        HasTieBreakQuestions = true,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(BallotQuestion11IdBundVote2InContestBund),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                            new BallotQuestion
                            {
                                Number = 2,
                                Id = Guid.Parse(BallotQuestion12IdBundVote2InContestBund),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 2 Bund"),
                                Type = BallotQuestionType.CounterProposal,
                            },
                            new BallotQuestion
                            {
                                Number = 3,
                                Id = Guid.Parse(BallotQuestion13IdBundVote2InContestBund),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 3 Bund"),
                                Type = BallotQuestionType.CounterProposal,
                            },
                        },
                        TieBreakQuestions = new List<TieBreakQuestion>
                        {
                            new TieBreakQuestion
                            {
                                Id = Guid.Parse(BallotTieBreakQuestion11IdBundVote2InContestBund),
                                Number = 1,
                                Question1Number = 1,
                                Question2Number = 2,
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 vor Frage 2?"),
                            },
                            new TieBreakQuestion
                            {
                                Id = Guid.Parse(BallotTieBreakQuestion12IdBundVote2InContestBund),
                                Number = 2,
                                Question1Number = 1,
                                Question2Number = 3,
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 vor Frage 3?"),
                            },
                            new TieBreakQuestion
                            {
                                Id = Guid.Parse(BallotTieBreakQuestion13IdBundVote2InContestBund),
                                Number = 3,
                                Question1Number = 2,
                                Question2Number = 3,
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 2 vor Frage 3?"),
                            },
                        },
                    },
                    new Ballot
                    {
                        Id = Guid.Parse(BallotId2BundVote2InContestBund),
                        Position = 2,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(BallotQuestion21IdBundVote2InContestBund),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 2.1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote StGallenVoteInContestBund
        => new Vote
        {
            Id = Guid.Parse(IdStGallenVoteInContestBund),
            PoliticalBusinessNumber = "201",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Abst SG"),
            InternalDescription = "Abstimmung St. Gallen auf Urnengang Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdStGallenVoteInContestBund),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("35e3ba5c-4b03-485e-a816-b11590b34f90"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Bund"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote BundVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdBundVoteInContestStGallen),
            PoliticalBusinessNumber = "100",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Bund",
                (t, s) => t.ShortDescription = s,
                "Abst Bund"),
            InternalDescription = "Abstimmung Bund auf Urnengang St.Gallen",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdBund),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            BallotBundleSampleSizePercent = 0,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdBundVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("b86d3fb0-0a01-46cd-a616-c559bb17c57b"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 St. Gallen"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote UzwilVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdUzwilVoteInContestStGallen),
            PoliticalBusinessNumber = "166",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Uzwil",
                (t, s) => t.ShortDescription = s,
                "Abst Uzwil"),
            InternalDescription = "Abstimmung Uzwil auf Urnengang St.Gallen",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            BallotBundleSampleSizePercent = 10,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = true,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdUzwilVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(BallotQuestionIdUzwilVoteInContestStGallen),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Uzwil"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote StGallenVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdStGallenVoteInContestStGallen),
            PoliticalBusinessNumber = "155",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung St. Gallen",
                (t, s) => t.ShortDescription = s,
                "Abst SG"),
            InternalDescription = "Abstimmung St.Gallen auf Urnengang St.Gallen",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            BallotBundleSampleSizePercent = 50,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdStGallenVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("49e72672-bbaf-4724-ab82-ce19305f2be4"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 St. Gallen"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote GossauVoteInContestStGallen
        => new Vote
        {
            Id = Guid.Parse(IdGossauVoteInContestStGallen),
            PoliticalBusinessNumber = "321",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Gossau",
                (t, s) => t.ShortDescription = s,
                "Abst Gossau"),
            InternalDescription = "Abstimmung Gossau auf Urnengang St.Gallen",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdGossauVoteInContestStGallen),
                        Position = 1,
                        BallotType = BallotType.VariantsBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(BallotQuestion1IdGossauVoteInContestStGallen),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Gossau"),
                                Type = BallotQuestionType.MainBallot,
                            },
                            new BallotQuestion
                            {
                                Number = 2,
                                Id = Guid.Parse(BallotQuestion2IdGossauVoteInContestStGallen),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 2 Gossau"),
                                Type = BallotQuestionType.CounterProposal,
                            },
                        },
                        HasTieBreakQuestions = true,
                        TieBreakQuestions = new List<TieBreakQuestion>
                        {
                            new TieBreakQuestion
                            {
                                Id = Guid.Parse(TieBreakQuestionIdGossauVoteInContestStGallen),
                                Number = 1,
                                Question1Number = 1,
                                Question2Number = 2,
                                Translations = TranslationUtil.CreateTranslations<TieBreakQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Stichfrage 1 Gossau (Frage 1 vs Frage 2)"),
                            },
                        },
                    },
            },
        };

    public static Vote StGallenVoteInContestStGallenWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdStGallenVoteInContestStGallenWithoutChilds),
            PoliticalBusinessNumber = "500",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung St. Gallen 2",
                (t, s) => t.ShortDescription = s,
                "Abst SG2"),
            InternalDescription = "Abstimmung St.Gallen auf Urnengang St.Gallen ohne Vorlage und Optionen",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdStGallen),
            ContestId = Guid.Parse(ContestMockedData.IdStGallenEvoting),
            Active = false,
            BallotBundleSampleSizePercent = 0,
            AutomaticBallotBundleNumberGeneration = false,
            ResultEntry = VoteResultEntry.FinalResults,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
        };

    public static Vote GossauVoteInContestGossau
        => new Vote
        {
            Id = Guid.Parse(IdGossauVoteInContestGossau),
            PoliticalBusinessNumber = "324",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Gossau",
                (t, s) => t.ShortDescription = s,
                "Abst Gossau"),
            InternalDescription = "Abstimmung Gossau auf Urnengang Gossau mit E-Voting",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGossau),
            ContestId = Guid.Parse(ContestMockedData.IdGossau),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdGossauVoteInContestGossau),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse(BallotQuestionIdGossauVoteInContestGossau),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Gossau"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote UzwilVoteInContestUzwil
        => new Vote
        {
            Id = Guid.Parse(IdUzwilVoteInContestUzwilWithoutChilds),
            PoliticalBusinessNumber = "412",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Uwzil",
                (t, s) => t.ShortDescription = s,
                "Abst Uzwil"),
            InternalDescription = "Abstimmung Uzwil auf Urnengang Uzwil",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            ContestId = Guid.Parse(ContestMockedData.IdUzwilEvoting),
            Active = true,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
            Ballots = new List<Ballot>
            {
                    new Ballot
                    {
                        Id = Guid.Parse(BallotIdUzwilVoteInContestUzwil),
                        Position = 1,
                        BallotType = BallotType.StandardBallot,
                        BallotQuestions = new List<BallotQuestion>
                        {
                            new BallotQuestion
                            {
                                Number = 1,
                                Id = Guid.Parse("328188ad-0a94-4c7b-8391-0ad3d5738286"),
                                Translations = TranslationUtil.CreateTranslations<BallotQuestionTranslation>(
                                    (t, o) => t.Question = o,
                                    "Frage 1 Uzwil"),
                                Type = BallotQuestionType.MainBallot,
                            },
                        },
                    },
            },
        };

    public static Vote UzwilVoteInContestBundWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdUzwilVoteInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Uzwil",
                (t, s) => t.ShortDescription = s,
                "Abst Uzwil"),
            InternalDescription = "Abstimmung Uzwil auf Urnengang Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdUzwil),
            Active = false,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
        };

    public static Vote GenfVoteInContestBundWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdGenfVoteInContestBundWithoutChilds),
            PoliticalBusinessNumber = "714a",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Genf",
                (t, s) => t.ShortDescription = s,
                "Abst Genf"),
            InternalDescription = "Abstimmung Genf auf Urnengang Bund",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdGenf),
            ContestId = Guid.Parse(ContestMockedData.IdBundesurnengang),
            Active = false,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Electronically,
            EnforceReviewProcedureForCountingCircles = false,
        };

    public static Vote KircheVoteInContestKircheWithoutChilds
        => new Vote
        {
            Id = Guid.Parse(IdKircheVoteInContestKircheWithoutChilds),
            PoliticalBusinessNumber = "aaa",
            Translations = TranslationUtil.CreateTranslations<VoteTranslation>(
                (t, o) => t.OfficialDescription = o,
                "Abstimmung Kirche",
                (t, s) => t.ShortDescription = s,
                "Abst Kirche"),
            InternalDescription = "Abstimmung Kirche auf Urnengang Kirche",
            DomainOfInfluenceId = Guid.Parse(DomainOfInfluenceMockedData.IdKirchgemeinde),
            ContestId = Guid.Parse(ContestMockedData.IdKirche),
            Active = false,
            BallotBundleSampleSizePercent = 30,
            AutomaticBallotBundleNumberGeneration = true,
            ResultEntry = VoteResultEntry.Detailed,
            ResultAlgorithm = VoteResultAlgorithm.PopularMajority,
            EnforceResultEntryForCountingCircles = false,
            ReviewProcedure = VoteReviewProcedure.Physically,
            EnforceReviewProcedureForCountingCircles = true,
        };

    public static IEnumerable<Vote> All
    {
        get
        {
            yield return BundVoteInContestArchivedBund;
            yield return BundVoteInContestVergangenBund;
            yield return BundVoteInContestBund;
            yield return BundVote2InContestBund;
            yield return BundVoteInContestStGallen;
            yield return UzwilVoteInContestStGallen;
            yield return StGallenVoteInContestBund;
            yield return StGallenVoteInContestStGallen;
            yield return GossauVoteInContestStGallen;
            yield return StGallenVoteInContestStGallenWithoutChilds;
            yield return GossauVoteInContestGossau;
            yield return UzwilVoteInContestUzwil;
            yield return UzwilVoteInContestBundWithoutChilds;
            yield return GenfVoteInContestBundWithoutChilds;
            yield return KircheVoteInContestKircheWithoutChilds;
        }
    }

    public static async Task Seed(Func<Func<IServiceProvider, Task>, Task> runScoped)
    {
        var votes = All.ToList();
        await runScoped(async sp =>
        {
            var db = sp.GetRequiredService<DataContext>();

            foreach (var vote in votes)
            {
                var snapshotDomainOfInfluence = await db.DomainOfInfluences.FirstAsync(doi =>
                    doi.BasisDomainOfInfluenceId == vote.DomainOfInfluenceId && doi.SnapshotContestId == vote.ContestId);
                vote.DomainOfInfluenceId = snapshotDomainOfInfluence.Id;
            }

            db.Votes.AddRange(votes);
            await db.SaveChangesAsync();

            var voteEndResultInitializer = sp.GetRequiredService<VoteEndResultInitializer>();
            foreach (var vote in votes)
            {
                await voteEndResultInitializer.RebuildForVote(vote.Id, ContestMockedData.TestingPhaseEnded(vote.ContestId));
            }
        });

        await runScoped(async sp =>
        {
            var builder = sp.GetRequiredService<SimplePoliticalBusinessBuilder<Vote>>();
            foreach (var vote in votes)
            {
                await builder.Create(vote);
            }
        });

        await VoteResultMockedData.Seed(runScoped, votes);
    }
}
