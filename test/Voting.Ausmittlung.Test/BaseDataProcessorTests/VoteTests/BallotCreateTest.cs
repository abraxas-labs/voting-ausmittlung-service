// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Test.MockedData;
using Voting.Lib.Common;
using Voting.Lib.Testing.Utils;
using Xunit;
using SharedProto = Abraxas.Voting.Basis.Shared.V1;

namespace Voting.Ausmittlung.Test.BaseDataProcessorTests.VoteTests;

public class BallotCreateTest : VoteProcessorBaseTest
{
    public BallotCreateTest(TestApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task TestCreated()
    {
        var ballotId = "0afc89f8-fc84-4a86-ace6-2cedfb5f8033";
        var ballotGuid = Guid.Parse(ballotId);
        await TestEventPublisher.Publish(
            new BallotCreated
            {
                Ballot = new BallotEventData
                {
                    Id = ballotId,
                    Position = 3,
                    VoteId = VoteMockedData.IdGossauVoteInContestGossau,
                    BallotType = SharedProto.BallotType.VariantsBallot,
                    HasTieBreakQuestions = true,
                    BallotQuestions =
                    {
                            new BallotQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("Variante 1") },
                                Type = SharedProto.BallotQuestionType.MainBallot,
                                FederalIdentificationString = "111",
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2") },
                                Type = SharedProto.BallotQuestionType.Variant,
                                FederalIdentificationString = "222",
                            },
                            new BallotQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("Variante 3") },
                                Type = SharedProto.BallotQuestionType.Variant,
                                FederalIdentificationString = "333",
                            },
                    },
                    TieBreakQuestions =
                    {
                            new TieBreakQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V2") },
                                Question1Number = 1,
                                Question2Number = 2,
                                FederalIdentificationString = "444",
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V3") },
                                Question1Number = 1,
                                Question2Number = 3,
                                FederalIdentificationString = "555",
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V2/V3") },
                                Question1Number = 2,
                                Question2Number = 3,
                                FederalIdentificationString = "666",
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
                             && br.BallotId == ballotGuid)
                .Include(r => r.QuestionResults).ThenInclude(x => x.Question).ThenInclude(x => x.Translations)
                .Include(r => r.TieBreakQuestionResults).ThenInclude(x => x.Question).ThenInclude(x => x.Translations)
                .ToListAsync(),
            Languages.German);
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

        questions[0].Question.Question.Should().Be("Variante 1 de");
        questions[1].Question.Question.Should().Be("Variante 2 de");
        questions[2].Question.Question.Should().Be("Variante 3 de");

        tieBreakQuestions[0].Question.Question.Should().Be("TieBreak V1/V2 de");
        tieBreakQuestions[1].Question.Question.Should().Be("TieBreak V1/V3 de");
        tieBreakQuestions[2].Question.Question.Should().Be("TieBreak V2/V3 de");
    }

    [Fact]
    public async Task TestWithDeprecatedFederalIdentification()
    {
        var ballotId = "0afc89f8-fc84-4a86-ace6-2cedfb5f8033";
        var ballotGuid = Guid.Parse(ballotId);
        await TestEventPublisher.Publish(
            new BallotCreated
            {
                Ballot = new BallotEventData
                {
                    Id = ballotId,
                    Position = 3,
                    VoteId = VoteMockedData.IdGossauVoteInContestGossau,
                    BallotType = SharedProto.BallotType.VariantsBallot,
                    HasTieBreakQuestions = true,
                    BallotQuestions =
                    {
                            new BallotQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("Variante 1") },
                                Type = SharedProto.BallotQuestionType.MainBallot,
#pragma warning disable CS0612
                                FederalIdentification = 111,
#pragma warning restore CS0612
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2") },
                                Type = SharedProto.BallotQuestionType.Variant,
#pragma warning disable CS0612
                                FederalIdentification = 222,
#pragma warning restore CS0612
                            },
                            new BallotQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("Variante 3") },
                                Type = SharedProto.BallotQuestionType.Variant,
#pragma warning disable CS0612
                                FederalIdentification = 333,
#pragma warning restore CS0612
                            },
                    },
                    TieBreakQuestions =
                    {
                            new TieBreakQuestionEventData
                            {
                                Number = 1,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V2") },
                                Question1Number = 1,
                                Question2Number = 2,
#pragma warning disable CS0612
                                FederalIdentification = 444,
#pragma warning restore CS0612
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V3") },
                                Question1Number = 1,
                                Question2Number = 3,
#pragma warning disable CS0612
                                FederalIdentification = 555,
#pragma warning restore CS0612
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V2/V3") },
                                Question1Number = 2,
                                Question2Number = 3,
#pragma warning disable CS0612
                                FederalIdentification = 666,
#pragma warning restore CS0612
                            },
                    },
                },
            });

        var ballotQuestions = await RunOnDb(
            db => db.BallotQuestions
                .Where(x => x.BallotId == ballotGuid)
                .OrderBy(x => x.Number)
                .ToListAsync());

        var tieBreakQuestions = await RunOnDb(
            db => db.TieBreakQuestions
                .Where(x => x.BallotId == ballotGuid)
                .OrderBy(x => x.Number)
                .ToListAsync());

        ballotQuestions.Count.Should().Be(3);
        ballotQuestions[0].FederalIdentification.Should().Be("111");
        ballotQuestions[1].FederalIdentification.Should().Be("222");
        ballotQuestions[2].FederalIdentification.Should().Be("333");
        tieBreakQuestions.Count.Should().Be(3);
        tieBreakQuestions[0].FederalIdentification.Should().Be("444");
        tieBreakQuestions[1].FederalIdentification.Should().Be("555");
        tieBreakQuestions[2].FederalIdentification.Should().Be("666");
    }
}
