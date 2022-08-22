// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Globalization;
using System.Linq;
using eCH_0110_4_0;
using eCH_0155_4_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Ballot = Voting.Ausmittlung.Data.Models.Ballot;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteResultMapping
{
    private const string UnknownDescriptionFallback = "?";

    internal static VoteResultType ToEchVoteResult(this VoteResult voteResult)
    {
        return new VoteResultType
        {
            Vote = voteResult.Vote.ToEchVote(),
            ballotResult = voteResult.Results.OrderBy(r => r.Ballot.Position).Select(r => r.ToEchBallotResult()).ToArray(),
            CountOfVotersInformation = new CountOfVotersInformationType { CountOfVotersTotal = voteResult.TotalCountOfVoters.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static BallotResultType ToEchBallotResult(this BallotResult ballotResult)
    {
        var ballot = ballotResult.Ballot;

        // The ballot description is optional in VOTING, but not in eCH
        var ballotDescriptionInfos = ballot.Translations
            .Where(t => !string.IsNullOrEmpty(t.Description))
            .Select(t => BallotDescriptionInfo.Create(t.Language, t.Description, null))
            .ToList();

        if (ballotDescriptionInfos.Count == 0)
        {
            ballotDescriptionInfos = Languages.All
                .Select(l => BallotDescriptionInfo.Create(l, UnknownDescriptionFallback, null))
                .ToList();
        }

        var ballotDescription = BallotDescriptionInformation.Create(ballotDescriptionInfos);

        return new BallotResultType
        {
            BallotDescription = ballotDescription,
            BallotPosition = ballot.Position.ToString(CultureInfo.InvariantCulture),
            BallotIdentification = ballot.Id.ToString(),
            CountOfAccountedBallotsTotal = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalAccountedBallots),
            CountOfReceivedBallotsTotal = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalReceivedBallots),
            CountOfUnaccountedBallotsTotal = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalUnaccountedBallots),
            CountOfUnaccountedBlankBallots = ResultDetailFromTotal(ballotResult.CountOfVoters.ConventionalBlankBallots.GetValueOrDefault()),
            CountOfUnaccountedInvalidBallots = ResultDetailFromTotal(ballotResult.CountOfVoters.ConventionalInvalidBallots.GetValueOrDefault()),
            Item = ballot.BallotType == BallotType.StandardBallot
                ? (object)ballotResult.QuestionResults.First().ToEchStandardBallotResult()
                : ballotResult.ToEchVariantBallotResult(),
        };
    }

    private static StandardBallotResultType ToEchStandardBallotResult(this BallotQuestionResult questionResult)
    {
        return new StandardBallotResultType
        {
            QuestionIdentification = questionResult.QuestionId.ToString(),
            CountOfAnswerEmpty = ResultDetailFromTotal(questionResult.TotalCountOfAnswerUnspecified),
            CountOfAnswerInvalid = ResultDetailFromTotal(0),
            CountOfAnswerYes = ResultDetailFromTotal(questionResult.TotalCountOfAnswerYes),
            CountOfAnswerNo = ResultDetailFromTotal(questionResult.TotalCountOfAnswerNo),
        };
    }

    private static VariantBallotResultType ToEchVariantBallotResult(this BallotResult ballotResult)
    {
        return new VariantBallotResultType
        {
            questionInformation = ballotResult.QuestionResults.OrderBy(r => r.Question.Number).Select(r => r.ToEchStandardBallotResult()).ToArray(),
            tieBreak = ballotResult.TieBreakQuestionResults.OrderBy(r => r.Question.Number).Select(r => r.ToEchTieBreakResult(ballotResult.Ballot)).ToArray(),
        };
    }

    private static TieBreak ToEchTieBreakResult(this TieBreakQuestionResult tieBreakQuestionResult, Ballot ballot)
    {
        var countInFavorQ1 = tieBreakQuestionResult.ToEchCountInFavorOf(true, ballot);
        var countInFavorQ2 = tieBreakQuestionResult.ToEchCountInFavorOf(false, ballot);

        return new TieBreak
        {
            QuestionIdentification = tieBreakQuestionResult.QuestionId.ToString(),
            CountInFavourOf = new[] { countInFavorQ1, countInFavorQ2 },
            CountOfAnswerEmpty = ResultDetailFromTotal(tieBreakQuestionResult.TotalCountOfAnswerUnspecified),
            CountOfAnswerInvalid = ResultDetailFromTotal(0),
        };
    }

    private static CountInFavourOf ToEchCountInFavorOf(this TieBreakQuestionResult tieBreakQuestionResult, bool q1, Ballot ballot)
    {
        var count = q1
            ? tieBreakQuestionResult.TotalCountOfAnswerQ1
            : tieBreakQuestionResult.TotalCountOfAnswerQ2;
        var questionNumber = q1
            ? tieBreakQuestionResult.Question.Question1Number
            : tieBreakQuestionResult.Question.Question2Number;

        return new CountInFavourOf
        {
            CountOfValidAnswers = ResultDetailFromTotal(count),
            QuestionIdentification = ballot.BallotQuestions.First(q => q.Number == questionNumber).Id.ToString(),
        };
    }

    private static ResultDetailType ResultDetailFromTotal(int total)
    {
        return new ResultDetailType
        {
            Total = total.ToString(CultureInfo.InvariantCulture),
        };
    }
}
