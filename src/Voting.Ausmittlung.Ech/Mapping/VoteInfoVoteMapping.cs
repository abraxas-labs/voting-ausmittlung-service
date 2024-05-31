// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoVoteMapping
{
    internal static IEnumerable<VoteInfoType> ToVoteInfoEchVote(this Ballot ballot)
    {
        ballot.OrderQuestions();

        foreach (var (question, i) in ballot.BallotQuestions.Select((question, i) => (question, i)))
        {
            var type = i switch
            {
                0 => VoteSubTypeType.Item1,
                1 => VoteSubTypeType.Item2,
                _ => VoteSubTypeType.Item4,
            };
            yield return ToVoteInfoEchVote(question, type);
        }

        foreach (var (question, i) in ballot.TieBreakQuestions.Select((question, i) => (question, i)))
        {
            var type = i switch
            {
                0 => VoteSubTypeType.Item3,
                1 => VoteSubTypeType.Item5,
                _ => VoteSubTypeType.Item6,
            };
            yield return ToVoteInfoEchVote(question, type);
        }
    }

    private static VoteInfoType ToVoteInfoEchVote(BallotQuestion question, VoteSubTypeType type)
    {
        var titleInfos = question.Translations
            .OrderBy(t => t.Language)
            .Select(t => new VoteTitleInformationType
            {
                Language = t.Language,
                VoteTitle = t.Question,
            });

        return new VoteInfoType
        {
            Vote = ToVoteTypeEchVote(question.Id, titleInfos, question.Ballot, type),
            CountingCircleInfo = question.Results
                .Where(r => r.BallotResult.VoteResult.Published)
                .OrderBy(r => r.BallotResult.VoteResult.CountingCircle.Name)
                .Select(r => ToCountingCircleInfo(
                    r.BallotResult,
                    r.TotalCountOfAnswerYes,
                    r.TotalCountOfAnswerNo,
                    r.TotalCountOfAnswerUnspecified))
                .ToList(),
        };
    }

    private static VoteInfoType ToVoteInfoEchVote(TieBreakQuestion question, VoteSubTypeType type)
    {
        var titleInfos = question.Translations
            .OrderBy(t => t.Language)
            .Select(t => new VoteTitleInformationType
            {
                Language = t.Language,
                VoteTitle = t.Question,
            });

        return new VoteInfoType
        {
            Vote = ToVoteTypeEchVote(question.Id, titleInfos, question.Ballot, type),
            CountingCircleInfo = question.Results
                .Where(r => r.BallotResult.VoteResult.Published)
                .OrderBy(r => r.BallotResult.VoteResult.CountingCircle.Name)
                .Select(r => ToCountingCircleInfo(
                    r.BallotResult,
                    r.TotalCountOfAnswerQ1,
                    r.TotalCountOfAnswerQ2,
                    r.TotalCountOfAnswerUnspecified))
                .ToList(),
        };
    }

    private static VoteType ToVoteTypeEchVote(
        Guid questionId,
        IEnumerable<VoteTitleInformationType> titleInfos,
        Ballot ballot,
        VoteSubTypeType type)
    {
        return new VoteType
        {
            PollingDay = ballot.Vote.Contest.Date,
            DomainOfInfluence = ballot.Vote.DomainOfInfluence.ToEchDomainOfInfluence(),
            DecisiveMajority = ballot.Vote.ResultAlgorithm.ToDecisiveMajorityType(),
            VoteIdentification = questionId.ToString(),
            MainVoteIdentification = ballot.HasTieBreakQuestions ? ballot.Id.ToString() : null,
            VoteSubType = type,
            VoteTitleInformation = titleInfos.ToList(),
        };
    }

    private static CountingCircleInfoType ToCountingCircleInfo(
        BallotResult ballotResult,
        int answersYes,
        int answersNo,
        int answersUnspecified)
    {
        return new CountingCircleInfoType
        {
            CountingCircle = ballotResult.VoteResult.CountingCircle.ToEch0252CountingCircle(),
            ResultData = new ResultDataType
            {
                CountOfVotersInformation =
                    new CountOfVotersInformationType
                    {
                        CountOfVotersTotal = (uint)ballotResult.VoteResult.TotalCountOfVoters,
                    },
                FullyCountedTrue = ballotResult.VoteResult.SubmissionDoneTimestamp.HasValue,
                ReleasedTimestamp = ballotResult.VoteResult.SubmissionDoneTimestamp,
                LockoutTimestamp = ballotResult.VoteResult.AuditedTentativelyTimestamp,
                VoterTurnout = ballotResult.CountOfVoters.VoterParticipation,
                ReceivedVotes = (uint)ballotResult.CountOfVoters.TotalReceivedBallots,
                ReceivedBlankVotes = (uint)ballotResult.CountOfVoters.TotalBlankBallots,
                ReceivedInvalidVotes = (uint)ballotResult.CountOfVoters.TotalInvalidBallots,
                ReceivedValidVotes = (uint)ballotResult.CountOfVoters.TotalAccountedBallots,
                CountOfNoVotes = (uint)answersNo,
                CountOfYesVotes = (uint)answersYes,
                CountOfVotesWithoutAnswer = (uint)answersUnspecified,
            },
        };
    }

    private static DecisiveMajorityType ToDecisiveMajorityType(this VoteResultAlgorithm resultAlgorithm)
    {
        return resultAlgorithm switch
        {
            VoteResultAlgorithm.PopularMajority => DecisiveMajorityType.Item1,
            VoteResultAlgorithm.CountingCircleMajority => DecisiveMajorityType.Item2,
            VoteResultAlgorithm.CountingCircleUnanimity => DecisiveMajorityType.Item3,
            VoteResultAlgorithm.PopularAndCountingCircleMajority => DecisiveMajorityType.Item4,
            _ => throw new InvalidOperationException("Invalid result algorithm"),
        };
    }
}
