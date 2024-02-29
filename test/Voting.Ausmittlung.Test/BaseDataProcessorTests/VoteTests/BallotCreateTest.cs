// (c) Copyright 2024 by Abraxas Informatik AG
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
                            },
                            new BallotQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("Variante 2") },
                            },
                            new BallotQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("Variante 3") },
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
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 2,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V1/V3") },
                                Question1Number = 1,
                                Question2Number = 3,
                            },
                            new TieBreakQuestionEventData
                            {
                                Number = 3,
                                Question = { LanguageUtil.MockAllLanguages("TieBreak V2/V3") },
                                Question1Number = 2,
                                Question2Number = 3,
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
}
