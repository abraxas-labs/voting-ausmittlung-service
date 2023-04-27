// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using eCH_0222_1_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech.Utils;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteRawDataMapping
{
    public static EVotingVoteResult ToEVotingVote(this VoteRawDataType voteRawData, string basisCountingCircleId)
    {
        var voteId = GuidParser.Parse(voteRawData.VoteIdentification);
        var ballotResults = voteRawData.BallotRawData
            .GroupBy(x => x.BallotIdentification)
            .Select(x => ToEVotingVoteBallotResult(x.Key, x.ToList()))
            .ToList();
        return new EVotingVoteResult(voteId, basisCountingCircleId, ballotResults);
    }

    internal static VoteRawDataType ToEchVoteRawData(this VoteResult voteResult)
    {
        return new VoteRawDataType
        {
            VoteIdentification = voteResult.Vote.Id.ToString(),
            BallotRawData = CreateBallotRawData(voteResult),
        };
    }

    private static VoteBallotRawData[] CreateBallotRawData(VoteResult voteResult)
    {
        // Export is only allowed for detailed result entry. This kind of result only have one variant ballot result.
        var ballotResult = voteResult.Results.First();

        return ballotResult.Bundles
            .Where(b => b.State == BallotBundleState.Reviewed)
            .SelectMany(b => b.Ballots)
            .OrderBy(x => x.Bundle.Number)
            .ThenBy(x => x.Number)
            .Select((ballot, index) => ballot.ToEchVoteBallotRawData(index))
            .ToArray();
    }

    private static VoteBallotRawData ToEchVoteBallotRawData(this VoteResultBallot voteResultBallot, int index)
    {
        return new VoteBallotRawData
        {
            BallotIdentification = voteResultBallot.Bundle.BallotResult.BallotId.ToString(),
            BallotCasted = new VoteBallotCasted
            {
                // ballot casted number require 1 based index
                BallotCastedNumber = (index + 1).ToString(CultureInfo.InvariantCulture),
                QuestionRawData = CreateVoteBallotCastedQuestionRawData(voteResultBallot),
            },
        };
    }

    private static VoteBallotCastedQuestionRawData[] CreateVoteBallotCastedQuestionRawData(this VoteResultBallot voteResultBallot)
    {
        return voteResultBallot.QuestionAnswers
            .OrderBy(x => x.Question.Number)
            .Select(ToEchVoteBallotCastedQuestionRawData)
            .Concat(voteResultBallot.TieBreakQuestionAnswers
                .OrderBy(x => x.Question.Number)
                .Select(ToEchVoteBallotCastedQuestionRawData))
            .ToArray();
    }

    private static VoteBallotCastedQuestionRawData ToEchVoteBallotCastedQuestionRawData(this VoteResultBallotQuestionAnswer questionAnswer)
    {
        var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(questionAnswer.Question.BallotId, false, questionAnswer.Question.Number);
        return new VoteBallotCastedQuestionRawData
        {
            QuestionIdentification = questionId,
            Casted = new VoteBallotCastedQuestionRawDataCasted
            {
                CastedVote = ToCastedVote(questionAnswer.Answer),
            },
        };
    }

    private static VoteBallotCastedQuestionRawData ToEchVoteBallotCastedQuestionRawData(this VoteResultBallotTieBreakQuestionAnswer questionAnswer)
    {
        var questionId = BallotQuestionIdConverter.ToEchBallotQuestionId(questionAnswer.Question.BallotId, true, questionAnswer.Question.Number);
        return new VoteBallotCastedQuestionRawData
        {
            QuestionIdentification = questionId,
            Casted = new VoteBallotCastedQuestionRawDataCasted
            {
                CastedVote = ToCastedVote(questionAnswer.Answer),
            },
        };
    }

    private static string ToCastedVote(BallotQuestionAnswer answer)
    {
        return answer switch
        {
            BallotQuestionAnswer.Yes => "1",
            BallotQuestionAnswer.No => "2",
            BallotQuestionAnswer.Unspecified => "3",
            _ => throw new InvalidOperationException($"Cannot map {answer} to a casted vote"),
        };
    }

    private static string ToCastedVote(TieBreakQuestionAnswer answer)
    {
        return answer switch
        {
            TieBreakQuestionAnswer.Q1 => "1",
            TieBreakQuestionAnswer.Q2 => "2",
            TieBreakQuestionAnswer.Unspecified => "3",
            _ => throw new InvalidOperationException($"Cannot map {answer} to a casted vote"),
        };
    }

    private static BallotQuestionAnswer ToBallotQuestionAnswer(string castedVote)
    {
        return castedVote switch
        {
            "1" => BallotQuestionAnswer.Yes,
            "2" => BallotQuestionAnswer.No,
            "3" => BallotQuestionAnswer.Unspecified,
            _ => throw new InvalidOperationException($"Cannot map {castedVote} to a ballot question answer"),
        };
    }

    private static TieBreakQuestionAnswer ToTieBreakQuestionAnswer(string castedVote)
    {
        return castedVote switch
        {
            "1" => TieBreakQuestionAnswer.Q1,
            "2" => TieBreakQuestionAnswer.Q2,
            "3" => TieBreakQuestionAnswer.Unspecified,
            _ => throw new InvalidOperationException($"Cannot map {castedVote} to a tie break question answer"),
        };
    }

    private static EVotingVoteBallotResult ToEVotingVoteBallotResult(string ballotKey, IEnumerable<VoteBallotRawData> ballotRawDatas)
    {
        var ballotId = GuidParser.Parse(ballotKey);
        var ballots = ballotRawDatas.Select(x => x.BallotCasted.ToEVotingVoteBallot()).ToList();
        return new EVotingVoteBallotResult(ballotId, ballots);
    }

    private static EVotingVoteBallot ToEVotingVoteBallot(this VoteBallotCasted voteBallotCasted)
    {
        var questions = voteBallotCasted
            .QuestionRawData
            .Select(x => (Parsed: BallotQuestionIdConverter.FromEchBallotQuestionId(x.QuestionIdentification), x.Casted));

        var questionAnswers =
            (IReadOnlyCollection<EVotingVoteBallotQuestionAnswer>?)questions
            .Where(x => !x.Parsed.IsTieBreakQuestion)
            .Select(x => new EVotingVoteBallotQuestionAnswer(x.Parsed.QuestioNumber, ToBallotQuestionAnswer(x.Casted.CastedVote)))
            .ToList()
            ?? Array.Empty<EVotingVoteBallotQuestionAnswer>();

        var tieBreakQuestionAnswers =
            (IReadOnlyCollection<EVotingVoteBallotTieBreakQuestionAnswer>?)questions
            .Where(x => x.Parsed.IsTieBreakQuestion)
            .Select(x => new EVotingVoteBallotTieBreakQuestionAnswer(x.Parsed.QuestioNumber, ToTieBreakQuestionAnswer(x.Casted.CastedVote)))
            .ToList()
            ?? Array.Empty<EVotingVoteBallotTieBreakQuestionAnswer>();

        return new EVotingVoteBallot(questionAnswers, tieBreakQuestionAnswers);
    }
}
