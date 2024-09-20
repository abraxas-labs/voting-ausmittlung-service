// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class BallotUpdateTest : VoteProcessorBaseTest
{
    public BallotUpdateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestUpdated()
    {
        await TestEventPublisher.Publish(
            new BallotUpdated
            {
                Ballot = new BallotEventData
                {
                    Id = VoteMockedData.BallotIdGossauVoteInContestGossau,
                    Position = 3,
                    VoteId = VoteMockedData.IdGossauVoteInContestGossau,
                    BallotType = SharedProto.BallotType.VariantsBallot,
                    HasTieBreakQuestions = true,
                    BallotQuestions =
                    {
                            new BallotQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("Variante 1X") },
                                Type = SharedProto.BallotQuestionType.MainBallot,
                                FederalIdentification = 111,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2X") },
                                Type = SharedProto.BallotQuestionType.Variant,
                                FederalIdentification = 222,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("Variante 3X") },
                                Type = SharedProto.BallotQuestionType.Variant,
                                FederalIdentification = 333,
                            },
                    },
                    TieBreakQuestions =
                    {
                            new TieBreakQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V2X") },
                                Question1Number = 1,
                                Question2Number = 2,
                                FederalIdentification = 444,
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V3X") },
                                Question1Number = 1,
                                Question2Number = 3,
                                FederalIdentification = 555,
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V2/V3X") },
                                Question1Number = 2,
                                Question2Number = 3,
                                FederalIdentification = 666,
                            },
                    },
                },
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau), true);
        data.MatchSnapshot();

        var results = await RunOnDb(
            db => db.BallotResults
                .AsSplitQuery()
                .Where(br => br.VoteResult.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau)
                             && br.BallotId == Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestGossau))
                .Include(r => r.QuestionResults)
                    .ThenInclude(x => x.Question).ThenInclude(x => x.Translations)
                .Include(r => r.QuestionResults)
                    .ThenInclude(x => x.ConventionalSubTotal)
                .Include(r => r.TieBreakQuestionResults)
                    .ThenInclude(x => x.Question).ThenInclude(x => x.Translations)
                .ToListAsync(),
            Languages.Italian);
        results.Count.Should().Be(1);

        var result = results[0];
        result.QuestionResults.Count.Should().Be(3);
        result.TieBreakQuestionResults.Count.Should().Be(3);

        var questions = result.QuestionResults
            .OrderBy(qr => qr.Question.Number)
            .ToList();
        var tieBreakQuestions = result.TieBreakQuestionResults
            .OrderBy(tr => tr.Question.Number)
            .ToList();

        questions[0].Question.Question.Should().Be("Variante 1X it");
        questions[1].Question.Question.Should().Be("Variante 2X it");
        questions[2].Question.Question.Should().Be("Variante 3X it");

        tieBreakQuestions[0].Question.Question.Should().Be("TieBreak V1/V2X it");
        tieBreakQuestions[1].Question.Question.Should().Be("TieBreak V1/V3X it");
        tieBreakQuestions[2].Question.Question.Should().Be("TieBreak V2/V3X it");

        var existingQuestionResult = result.QuestionResults.First(x => x.Question.Number == 1);
        existingQuestionResult.TotalCountOfAnswerYes.Should().NotBe(0);
        existingQuestionResult.TotalCountOfAnswerNo.Should().NotBe(0);
    }

    [Fact]
    public async Task TestUpdatedWithDeletedQuestions()
    {
        await TestEventPublisher.Publish(
            new BallotUpdated
            {
                Ballot = new BallotEventData
                {
                    Id = VoteMockedData.BallotIdGossauVoteInContestGossau,
                    Position = 3,
                    VoteId = VoteMockedData.IdGossauVoteInContestGossau,
                    BallotType = SharedProto.BallotType.VariantsBallot,
                    HasTieBreakQuestions = true,
                    BallotQuestions =
                    {
                            new BallotQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("Variante 1X") },
                                Type = SharedProto.BallotQuestionType.MainBallot,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2X") },
                                Type = SharedProto.BallotQuestionType.Variant,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("Variante 3X") },
                                Type = SharedProto.BallotQuestionType.Variant,
                            },
                    },
                    TieBreakQuestions =
                    {
                            new TieBreakQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V2X") },
                                Question1Number = 1,
                                Question2Number = 2,
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V3X") },
                                Question1Number = 1,
                                Question2Number = 3,
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V2/V3X") },
                                Question1Number = 2,
                                Question2Number = 3,
                            },
                    },
                },
            },
            new BallotUpdated
            {
                Ballot = new BallotEventData
                {
                    Id = VoteMockedData.BallotIdGossauVoteInContestGossau,
                    Position = 3,
                    VoteId = VoteMockedData.IdGossauVoteInContestGossau,
                    BallotType = SharedProto.BallotType.VariantsBallot,
                    HasTieBreakQuestions = true,
                    BallotQuestions =
                    {
                            new BallotQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("Variante 1X") },
                                Type = SharedProto.BallotQuestionType.MainBallot,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2X") },
                                Type = SharedProto.BallotQuestionType.Variant,
                            },
                    },
                    TieBreakQuestions =
                    {
                            new TieBreakQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V2X") },
                                Question1Number = 1,
                                Question2Number = 2,
                            },
                    },
                },
            });

        var results = await RunOnDb(
            db => db.BallotResults
                .AsSplitQuery()
                .Where(br => br.VoteResult.VoteId == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau)
                             && br.BallotId == Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestGossau))
                .Include(r => r.QuestionResults)
                    .ThenInclude(x => x.Question).ThenInclude(x => x.Translations)
                .Include(r => r.QuestionResults)
                    .ThenInclude(x => x.ConventionalSubTotal)
                .Include(r => r.TieBreakQuestionResults)
                    .ThenInclude(x => x.Question).ThenInclude(x => x.Translations)
                .Include(r => r.TieBreakQuestionResults)
                    .ThenInclude(x => x.ConventionalSubTotal)
                .ToListAsync(),
            Languages.Italian);
        results.Count.Should().Be(1);

        var result = results[0];
        result.QuestionResults.Count.Should().Be(2);
        result.TieBreakQuestionResults.Count.Should().Be(1);

        var existingQuestionResult = result.QuestionResults.First(x => x.Question.Number == 1);
        existingQuestionResult.TotalCountOfAnswerYes.Should().NotBe(0);
        existingQuestionResult.TotalCountOfAnswerNo.Should().NotBe(0);
    }

    [Fact]
    public async Task TestUpdatedShouldSetDefaultValues()
    {
        await TestEventPublisher.Publish(
            new BallotUpdated
            {
                Ballot = new BallotEventData
                {
                    Id = VoteMockedData.BallotIdGossauVoteInContestGossau,
                    Position = 3,
                    VoteId = VoteMockedData.IdGossauVoteInContestGossau,
                    BallotType = SharedProto.BallotType.VariantsBallot,
                    HasTieBreakQuestions = true,
                    BallotQuestions =
                    {
                            new BallotQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("Variante 1X") },
                                Type = SharedProto.BallotQuestionType.Unspecified,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2X") },
                                Type = SharedProto.BallotQuestionType.Unspecified,
                            },
                            new BallotQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("Variante 3X") },
                                Type = SharedProto.BallotQuestionType.Unspecified,
                            },
                    },
                },
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestGossau), true);
        data.MatchSnapshot();
    }

    [Fact]
    public async Task TestUpdatedAfterTestingPhaseEnded()
    {
        await TestEventPublisher.Publish(
            new BallotAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                BallotQuestions =
                {
                        new BallotQuestionEventData
                        {
                            Number = 1,
                            Question = { LanguageUtil.MockAllLanguages("Frage 1 updated") },
                            Type = SharedProto.BallotQuestionType.MainBallot,
                        },
                        new BallotQuestionEventData
                        {
                            Number = 2,
                            Question = { LanguageUtil.MockAllLanguages("Frage 2 updated") },
                            Type = SharedProto.BallotQuestionType.CounterProposal,
                        },
                },
                TieBreakQuestions =
                {
                        new TieBreakQuestionEventData
                        {
                            Number = 1,
                            Question = { LanguageUtil.MockAllLanguages("Strichfrage updated") },
                            Question1Number = 1,
                            Question2Number = 2,
                        },
                },
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen), true);
        data.MatchSnapshot();

        var result = await RunOnDb(db => db.BallotResults
            .AsSplitQuery()
            .Include(x => x.QuestionResults)
                .ThenInclude(x => x.ConventionalSubTotal)
            .Include(x => x.TieBreakQuestionResults)
                .ThenInclude(x => x.ConventionalSubTotal)
            .Where(x => x.Id == AusmittlungUuidV5.BuildVoteBallotResult(
                Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestStGallen), CountingCircleMockedData.GuidGossau))
            .SingleAsync());

        result.QuestionResults.Count.Should().Be(2);
        foreach (var questionResult in result.QuestionResults)
        {
            questionResult.TotalCountOfAnswerYes.Should().NotBe(0);
            questionResult.TotalCountOfAnswerNo.Should().NotBe(0);
        }

        result.TieBreakQuestionResults.Count.Should().Be(1);
        var tieBreakQuestionResult = result.TieBreakQuestionResults.Single();
        tieBreakQuestionResult.TotalCountOfAnswerQ1.Should().NotBe(0);
        tieBreakQuestionResult.TotalCountOfAnswerQ2.Should().NotBe(0);
    }

    [Fact]
    public async Task TestUpdatedAfterTestingPhaseEndedShouldSetDefaultValues()
    {
        await TestEventPublisher.Publish(
            new BallotAfterTestingPhaseUpdated
            {
                EventInfo = GetMockedEventInfo(),
                Id = VoteMockedData.BallotIdGossauVoteInContestStGallen,
                BallotQuestions =
                {
                    new BallotQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Frage 1 updated") },
                        Type = SharedProto.BallotQuestionType.Unspecified,
                    },
                    new BallotQuestionEventData
                    {
                        Number = 2,
                        Question = { LanguageUtil.MockAllLanguages("Frage 2 updated") },
                        Type = SharedProto.BallotQuestionType.Unspecified,
                    },
                },
                TieBreakQuestions =
                {
                    new TieBreakQuestionEventData
                    {
                        Number = 1,
                        Question = { LanguageUtil.MockAllLanguages("Strichfrage updated") },
                        Question1Number = 1,
                        Question2Number = 2,
                    },
                },
            });

        var data = await GetData(x => x.Id == Guid.Parse(VoteMockedData.IdGossauVoteInContestStGallen), true);
        data.MatchSnapshot();

        var result = await RunOnDb(db => db.BallotResults
            .AsSplitQuery()
            .Include(x => x.QuestionResults)
            .ThenInclude(x => x.ConventionalSubTotal)
            .Include(x => x.TieBreakQuestionResults)
            .ThenInclude(x => x.ConventionalSubTotal)
            .Where(x => x.Id == AusmittlungUuidV5.BuildVoteBallotResult(
                Guid.Parse(VoteMockedData.BallotIdGossauVoteInContestStGallen), CountingCircleMockedData.GuidGossau))
            .SingleAsync());

        result.QuestionResults.Count.Should().Be(2);
        foreach (var questionResult in result.QuestionResults)
        {
            questionResult.TotalCountOfAnswerYes.Should().NotBe(0);
            questionResult.TotalCountOfAnswerNo.Should().NotBe(0);
        }

        result.TieBreakQuestionResults.Count.Should().Be(1);
        var tieBreakQuestionResult = result.TieBreakQuestionResults.Single();
        tieBreakQuestionResult.TotalCountOfAnswerQ1.Should().NotBe(0);
        tieBreakQuestionResult.TotalCountOfAnswerQ2.Should().NotBe(0);
    }
}
