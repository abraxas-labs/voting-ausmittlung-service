// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ech0110_4_0;
using Ech0155_4_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech.Utils;
using Ballot = Voting.Ausmittlung.Data.Models.Ballot;
using BallotType = Voting.Ausmittlung.Data.Models.BallotType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteResultMapping
{
    private const string UnknownDescriptionFallback = "?";

    internal static VoteResultType ToEchVoteResult(this VoteResult voteResult)
    {
        return new VoteResultType
        {
            Vote = voteResult.Vote.ToEchVote(),
            BallotResult = voteResult.Results.OrderBy(r => r.Ballot.Position).Select(r => r.ToEchBallotResult()).ToList(),
            CountOfVotersInformation = new CountOfVotersInformationType { CountOfVotersTotal = voteResult.TotalCountOfVoters.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static BallotResultType ToEchBallotResult(this BallotResult ballotResult)
    {
        var ballot = ballotResult.Ballot;

        // The ballot description does not exist in VOTING, but is needed in eCH
        var ballotDescriptionInfos = Languages.All
            .Select(l => new BallotDescriptionInformationTypeBallotDescriptionInfo
            {
                Language = l,
                BallotDescriptionLong = UnknownDescriptionFallback,
            })
            .ToList();

        var ballotResultType = new BallotResultType
        {
            BallotDescription = ballotDescriptionInfos,
            BallotPosition = ballot.Position.ToString(CultureInfo.InvariantCulture),
            BallotIdentification = ballot.Id.ToString(),
            CountOfAccountedBallotsTotal = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalAccountedBallots),
            CountOfReceivedBallotsTotal = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalReceivedBallots),
            CountOfUnaccountedBallotsTotal = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalUnaccountedBallots),
            CountOfUnaccountedBlankBallots = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalBlankBallots),
            CountOfUnaccountedInvalidBallots = ResultDetailFromTotal(ballotResult.CountOfVoters.TotalInvalidBallots),
        };

        if (ballot.BallotType == BallotType.StandardBallot)
        {
            ballotResultType.StandardBallot = ballotResult.QuestionResults.First().ToEchStandardBallotResult();
        }
        else
        {
            ballotResultType.VariantBallot = ballotResult.ToEchVariantBallotResult();
        }

        return ballotResultType;
    }

    private static StandardBallotResultType ToEchStandardBallotResult(this BallotQuestionResult questionResult)
    {
        var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(questionResult.BallotResult.BallotId, false, questionResult.Question.Number);
        return new StandardBallotResultType
        {
            QuestionIdentification = questionId,
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
            QuestionInformation = ballotResult.QuestionResults.OrderBy(r => r.Question.Number).Select(r => r.ToEchStandardBallotResult()).ToList(),
            TieBreak = ballotResult.TieBreakQuestionResults.OrderBy(r => r.Question.Number).Select(r => r.ToEchTieBreakResult(ballotResult.Ballot)).ToList(),
        };
    }

    private static VariantBallotResultTypeTieBreak ToEchTieBreakResult(this TieBreakQuestionResult tieBreakQuestionResult, Ballot ballot)
    {
        var countInFavorQ1 = tieBreakQuestionResult.ToEchCountInFavorOf(true, ballot);
        var countInFavorQ2 = tieBreakQuestionResult.ToEchCountInFavorOf(false, ballot);

        var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, true, tieBreakQuestionResult.Question.Number);
        return new VariantBallotResultTypeTieBreak
        {
            QuestionIdentification = questionId,
            CountInFavourOf = new List<VariantBallotResultTypeTieBreakCountInFavourOf> { countInFavorQ1, countInFavorQ2 },
            CountOfAnswerEmpty = ResultDetailFromTotal(tieBreakQuestionResult.TotalCountOfAnswerUnspecified),
            CountOfAnswerInvalid = ResultDetailFromTotal(0),
        };
    }

    private static VariantBallotResultTypeTieBreakCountInFavourOf ToEchCountInFavorOf(this TieBreakQuestionResult tieBreakQuestionResult, bool q1, Ballot ballot)
    {
        var count = q1
            ? tieBreakQuestionResult.TotalCountOfAnswerQ1
            : tieBreakQuestionResult.TotalCountOfAnswerQ2;
        var questionNumber = q1
            ? tieBreakQuestionResult.Question.Question1Number
            : tieBreakQuestionResult.Question.Question2Number;

        var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(ballot.Id, false, questionNumber);
        return new VariantBallotResultTypeTieBreakCountInFavourOf
        {
            CountOfValidAnswers = ResultDetailFromTotal(count),
            QuestionIdentification = questionId,
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
