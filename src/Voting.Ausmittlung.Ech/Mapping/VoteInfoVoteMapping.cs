// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ech0252_2_0;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Common;
using VoteType = Ech0252_2_0.VoteType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteInfoVoteMapping
{
    private const string FederalIdentifier = "idBund";

    internal static IEnumerable<VoteInfoType> ToVoteInfoEchVote(
        this Ballot ballot,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates,
        Dictionary<Guid, ushort> sequenceBySuperiorAuthorityId)
    {
        ballot.OrderQuestions();

        string? mainQuestionId = null;
        var voteSubType = VoteSubTypeType.Item1;
        var titleInfos = new List<VoteTitleInformationType>();

        if (ballot.SubType != BallotSubType.Unspecified)
        {
            // This is a variant vote on multiple ballots
            voteSubType = ballot.SubType switch
            {
                BallotSubType.MainBallot => VoteSubTypeType.Item1,
                BallotSubType.CounterProposal1 => VoteSubTypeType.Item2,
                BallotSubType.CounterProposal2 => VoteSubTypeType.Item4,
                BallotSubType.Variant1 => VoteSubTypeType.Item2,
                BallotSubType.Variant2 => VoteSubTypeType.Item4,
                BallotSubType.TieBreak1 => VoteSubTypeType.Item3,
                BallotSubType.TieBreak2 => VoteSubTypeType.Item5,
                BallotSubType.TieBreak3 => VoteSubTypeType.Item6,
                _ => throw new InvalidOperationException($"Unsupported ballot sub type {ballot.SubType}"),
            };

            mainQuestionId = BuildQuestionId(ballot.Vote.Ballots.Single(x => x.Position == 1).Id);
            titleInfos = ballot.Translations
                .Where(t => t.Language == Languages.German)
                .Select(t => new VoteTitleInformationType
                {
                    Language = t.Language,
                    VoteTitle = t.OfficialDescription,
                    VoteTitleShort = t.ShortDescription,
                })
                .ToList();
        }
        else if (ballot.BallotType == BallotType.VariantsBallot)
        {
            // Variant vote on a single ballot
            mainQuestionId = BuildQuestionId(ballot.Id);
        }
        else
        {
            // This is a standard vote
            titleInfos = ballot.Vote.Translations
                .Where(t => t.Language == Languages.German)
                .Select(t => new VoteTitleInformationType
                {
                    Language = t.Language,
                    VoteTitle = t.OfficialDescription,
                    VoteTitleShort = t.ShortDescription,
                })
                .ToList();
        }

        foreach (var (question, i) in ballot.BallotQuestions.Select((question, i) => (question, i)))
        {
            if (ballot.BallotType == BallotType.VariantsBallot)
            {
                voteSubType = i switch
                {
                    0 => VoteSubTypeType.Item1,
                    1 => VoteSubTypeType.Item2,
                    _ => VoteSubTypeType.Item4,
                };
            }

            yield return ToVoteInfoEchVote(
                question,
                voteSubType,
                mainQuestionId,
                titleInfos,
                ctx,
                enabledResultStates,
                sequenceBySuperiorAuthorityId);
        }

        foreach (var (question, i) in ballot.TieBreakQuestions.Select((question, i) => (question, i)))
        {
            var type = i switch
            {
                0 => VoteSubTypeType.Item3,
                1 => VoteSubTypeType.Item5,
                _ => VoteSubTypeType.Item6,
            };
            yield return ToVoteInfoEchVote(
                question,
                type,
                mainQuestionId,
                ctx,
                enabledResultStates,
                sequenceBySuperiorAuthorityId);
        }
    }

    private static VoteInfoType ToVoteInfoEchVote(
        BallotQuestion question,
        VoteSubTypeType type,
        string? mainQuestionId,
        List<VoteTitleInformationType> titleInfos,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates,
        Dictionary<Guid, ushort> sequenceBySuperiorAuthorityId)
    {
        if (question.Ballot.BallotType == BallotType.VariantsBallot)
        {
            var voteTranslation = question.Ballot.Vote.Translations.Single(t => t.Language == Languages.German);

            titleInfos = question.Translations
                .Where(t => t.Language == Languages.German)
                .Select(t => new VoteTitleInformationType
                {
                    Language = t.Language,
                    VoteTitle = $"{voteTranslation.OfficialDescription} - {t.Question}",
                    VoteTitleShort = $"{voteTranslation.ShortDescription} - {t.Question}",
                })
                .ToList();
        }

        var questionId = BuildQuestionId(question.BallotId, question.Number);
        return new VoteInfoType
        {
            Vote = ToVoteTypeEchVote(questionId, titleInfos, question.Ballot, type, mainQuestionId, ctx, sequenceBySuperiorAuthorityId, question.FederalIdentification),
            CountingCircleInfo = question.Results
                .OrderBy(r => r.BallotResult.VoteResult.CountingCircle.Name)
                .Select(r => ToCountingCircleInfo(
                    r.BallotResult,
                    r.TotalCountOfAnswerYes,
                    r.TotalCountOfAnswerNo,
                    r.TotalCountOfAnswerUnspecified,
                    enabledResultStates))
                .ToList(),
        };
    }

    private static VoteInfoType ToVoteInfoEchVote(
        TieBreakQuestion question,
        VoteSubTypeType type,
        string? mainQuestionId,
        Ech0252MappingContext ctx,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates,
        Dictionary<Guid, ushort> sequenceBySuperiorAuthorityId)
    {
        var questionId = BuildQuestionId(question.BallotId, question.Number, true);
        var voteTranslation = question.Ballot.Vote.Translations.Single(t => t.Language == Languages.German);
        var titleInfos = question.Translations
            .Where(t => t.Language == Languages.German)
            .OrderBy(t => t.Language)
            .Select(t => new VoteTitleInformationType
            {
                Language = t.Language,
                VoteTitle = $"{voteTranslation.OfficialDescription} - {t.Question}",
                VoteTitleShort = $"{voteTranslation.ShortDescription} - {t.Question}",
            });

        return new VoteInfoType
        {
            Vote = ToVoteTypeEchVote(questionId, titleInfos, question.Ballot, type, mainQuestionId, ctx, sequenceBySuperiorAuthorityId, question.FederalIdentification),
            CountingCircleInfo = question.Results
                .OrderBy(r => r.BallotResult.VoteResult.CountingCircle.Name)
                .Select(r => ToCountingCircleInfo(
                    r.BallotResult,
                    r.TotalCountOfAnswerQ1,
                    r.TotalCountOfAnswerQ2,
                    r.TotalCountOfAnswerUnspecified,
                    enabledResultStates))
                .ToList(),
        };
    }

    private static VoteType ToVoteTypeEchVote(
        string questionId,
        IEnumerable<VoteTitleInformationType> titleInfos,
        Ballot ballot,
        VoteSubTypeType type,
        string? mainQuestionId,
        Ech0252MappingContext ctx,
        Dictionary<Guid, ushort> sequenceBySuperiorAuthorityId,
        int? federalIdentification)
    {
        var superiorAuthority = ctx.GetSuperiorAuthority(ballot.Vote.DomainOfInfluence.Id);
        var superiorAuthorityId = superiorAuthority?.Id ?? Guid.Empty;

        var previousSequence = sequenceBySuperiorAuthorityId.GetValueOrDefault(superiorAuthorityId, (ushort)0);
        var sequence = (ushort)(previousSequence + 1);
        sequenceBySuperiorAuthorityId[superiorAuthorityId] = sequence;

        return new VoteType
        {
            PollingDay = ballot.Vote.Contest.Date,
            SuperiorAuthority = superiorAuthority?.ToEchDomainOfInfluence(),
            DomainOfInfluence = ballot.Vote.DomainOfInfluence.ToEchDomainOfInfluence(),
            DecisiveMajority = ballot.Vote.ResultAlgorithm.ToDecisiveMajorityType(),
            VoteIdentification = questionId,
            MainVoteIdentification = mainQuestionId,
            VoteSubType = type,
            VoteTitleInformation = titleInfos.ToList(),
            Sequence = sequence,
            OtherIdentification = ToOtherIdentification(federalIdentification),
        };
    }

    private static CountingCircleInfoType ToCountingCircleInfo(
        BallotResult ballotResult,
        int answersYes,
        int answersNo,
        int answersUnspecified,
        IReadOnlyCollection<CountingCircleResultState>? enabledResultStates)
    {
        var hasResultData = ballotResult.VoteResult.Published
            && enabledResultStates?.Contains(ballotResult.VoteResult.State) != false;

        return new CountingCircleInfoType
        {
            CountingCircle = ballotResult.VoteResult.CountingCircle.ToEch0252CountingCircle(),
            ResultData = hasResultData
                ? new ResultDataType
                {
                    CountOfVotersInformation =
                        new CountOfVotersInformationType
                        {
                            CountOfVotersTotal = (uint)ballotResult.VoteResult.TotalCountOfVoters,
                        },
                    IsFullyCounted = ballotResult.VoteResult.SubmissionDoneTimestamp.HasValue,
                    ReleasedTimestamp = ballotResult.VoteResult.SubmissionDoneTimestamp,
                    LockoutTimestamp = ballotResult.VoteResult.AuditedTentativelyTimestamp,
                    VoterTurnout = VoteInfoCountingCircleResultMapping.DecimalToPercentage(ballotResult.CountOfVoters.VoterParticipation),
                    ReceivedVotes = (uint)ballotResult.CountOfVoters.TotalReceivedBallots,
                    ReceivedBlankVotes = (uint)ballotResult.CountOfVoters.TotalBlankBallots,
                    ReceivedInvalidVotes = (uint)ballotResult.CountOfVoters.TotalInvalidBallots,
                    ReceivedValidVotes = (uint)ballotResult.CountOfVoters.TotalAccountedBallots,
                    CountOfNoVotes = (uint)answersNo,
                    CountOfYesVotes = (uint)answersYes,
                    CountOfVotesWithoutAnswer = (uint)answersUnspecified,
                    NamedElement = VoteInfoCountingCircleResultMapping.GetNamedElements(ballotResult.VoteResult),
                    VotingCardInformation = ballotResult.VoteResult.CountingCircle.ToVoteInfoVotingCardInfo(ballotResult.VoteResult.Vote.DomainOfInfluence.Type),
                }
                : null,
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

    // Question ids are not stored in events, they are generated randomly during event processing
    // and thus are not persisted when events are replayed.
    // To get something that stays the same, the question number must be used.
    private static string BuildQuestionId(Guid ballotId, int questionNumber = 1, bool isTieBreakQuestion = false)
        => $"{ballotId}_{questionNumber}{(isTieBreakQuestion ? "_t" : string.Empty)}";

    private static List<NamedIdType> ToOtherIdentification(int? federalIdentification)
    {
        return federalIdentification.HasValue
            ? new List<NamedIdType>
            {
                new NamedIdType
                {
                    IdName = FederalIdentifier,
                    Id = federalIdentification.Value.ToString(CultureInfo.InvariantCulture),
                },
            }
            : new List<NamedIdType>();
    }
}
