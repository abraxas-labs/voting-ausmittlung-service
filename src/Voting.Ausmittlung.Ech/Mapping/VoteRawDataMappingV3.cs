// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0222_3_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;
using Voting.Lib.Ech.Utils;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteRawDataMappingV3
{
    public static VotingImportVoteResult ToEVotingVote(this VoteRawDataType voteRawData, string basisCountingCircleId)
    {
        var voteId = GuidParser.Parse(voteRawData.VoteIdentification);
        var ballotResults = voteRawData.BallotRawData
            .GroupBy(x => x.ElectronicBallotIdentification)
            .Select(x => ToEVotingVoteBallotResult(x.Key, x.ToList()))
            .ToList();
        return new VotingImportVoteResult(voteId, basisCountingCircleId, ballotResults);
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

    private static VotingImportVoteBallotResult ToEVotingVoteBallotResult(string ballotKey, IEnumerable<VoteRawDataTypeBallotRawData> ballotRawDatas)
    {
        var ballotId = GuidParser.Parse(ballotKey);
        var ballots = ballotRawDatas.Select(x => x.BallotCasted.ToEVotingVoteBallot()).ToList();
        return new VotingImportVoteBallotResult(ballotId, ballots);
    }

    private static VotingVoteBallot ToEVotingVoteBallot(this VoteRawDataTypeBallotRawDataBallotCasted voteBallotCasted)
    {
        var questions = voteBallotCasted
            .QuestionRawData
            .Select(x => (Parsed: BallotQuestionIdConverter.FromEchBallotQuestionId(x.QuestionIdentification), x.Casted))
            .ToArray();

        var questionAnswers =
            (IReadOnlyCollection<VotingImportVoteBallotQuestionAnswer>?)questions
            .Where(x => !x.Parsed.IsTieBreakQuestion)
            .Select(x => new VotingImportVoteBallotQuestionAnswer(x.Parsed.QuestioNumber, ToBallotQuestionAnswer(x.Casted.CastedVote)))
            .ToList()
            ?? Array.Empty<VotingImportVoteBallotQuestionAnswer>();

        var tieBreakQuestionAnswers =
            (IReadOnlyCollection<VotingImportVoteBallotTieBreakQuestionAnswer>?)questions
            .Where(x => x.Parsed.IsTieBreakQuestion)
            .Select(x => new VotingImportVoteBallotTieBreakQuestionAnswer(x.Parsed.QuestioNumber, ToTieBreakQuestionAnswer(x.Casted.CastedVote)))
            .ToList()
            ?? Array.Empty<VotingImportVoteBallotTieBreakQuestionAnswer>();

        return new VotingVoteBallot(questionAnswers, tieBreakQuestionAnswers);
    }
}
