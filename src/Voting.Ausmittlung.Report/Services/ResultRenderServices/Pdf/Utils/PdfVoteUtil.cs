// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfVoteUtil
{
    public static void SetLabels(IEnumerable<PdfVote> votes)
    {
        foreach (var vote in votes)
        {
            SetLabels(vote);
        }
    }

    public static void SetLabels(PdfVote vote)
    {
        var ballotEndResults = vote.EndResult?.BallotEndResults ?? new();
        var ballotDoiResults = vote.DomainOfInfluenceBallotResults ?? new List<PdfVoteBallotDomainOfInfluenceResult>();
        var ballotResults = vote.Results?.SelectMany(x => x.Results) ?? new List<PdfBallotResult>();

        foreach (var ballotEndResult in ballotEndResults)
        {
            SetLabels(
                ballotEndResult,
                x => x.QuestionEndResults?.Select(r => r.Question!),
                x => x.TieBreakQuestionEndResults?.Select(r => r.Question!));
        }

        foreach (var ballotDoiResult in ballotDoiResults)
        {
            SetLabels(ballotDoiResult);
        }

        foreach (var ballotResult in ballotResults)
        {
            SetLabels(
                ballotResult,
                x => x.QuestionResults?.Select(r => r.Question!),
                x => x.TieBreakQuestionResults?.Select(r => r.Question!));

            SetLabels(
                ballotResult,
                x => x.Ballot?.BallotQuestions,
                x => x.Ballot?.TieBreakQuestions);
        }

        SetResultLabel(vote);
    }

    public static void SetLabels<T>(
        T data,
        Func<T, IEnumerable<PdfBallotQuestion>?> questionsSelector,
        Func<T, IEnumerable<PdfTieBreakQuestion>?> tieBreakQuestionsSelector)
    {
        var questions = questionsSelector(data)?.ToList() ?? new List<PdfBallotQuestion>();
        var tieBreakQuestions = tieBreakQuestionsSelector(data)?.ToList() ?? new List<PdfTieBreakQuestion>();

        switch (questions.Count)
        {
            case 1:
                questions[0].Label = PdfVoteQuestionLabel.MainBallot;
                break;

            case 2:
                questions[0].Label = PdfVoteQuestionLabel.MainBallot;
                questions[1].Label = questions[1].Type == BallotQuestionType.CounterProposal ? PdfVoteQuestionLabel.CounterProposal : PdfVoteQuestionLabel.Variant;

                if (tieBreakQuestions.Count == 1)
                {
                    tieBreakQuestions[0].Label = PdfVoteQuestionLabel.TieBreak;
                }

                break;

            case 3:
                questions[0].Label = PdfVoteQuestionLabel.MainBallot;
                questions[1].Label = questions[1].Type == BallotQuestionType.CounterProposal ? PdfVoteQuestionLabel.CounterProposal1 : PdfVoteQuestionLabel.Variant1;
                questions[2].Label = questions[2].Type == BallotQuestionType.CounterProposal ? PdfVoteQuestionLabel.CounterProposal2 : PdfVoteQuestionLabel.Variant2;

                var tieBreakLabels = new[]
                {
                    PdfVoteQuestionLabel.TieBreak1,
                    PdfVoteQuestionLabel.TieBreak2,
                    PdfVoteQuestionLabel.TieBreak3,
                };

                for (var i = 0; i < tieBreakQuestions.Count; i++)
                {
                    tieBreakQuestions[i].Label = tieBreakLabels[i];
                }

                break;
        }
    }

    /// <summary>
    /// Sets the <see cref="PdfBallotEndResult.QuestionEndResultLabel"/> on all <see cref="PdfBallotEndResult"/>.
    /// Initializes the label bitmask with the question count (1 &lt;&lt; Count - 1).
    /// Then loops through all questions and sets the accepted bits to 1.
    /// <seealso cref="PdfBallotEndResultLabel"/>.
    /// </summary>
    /// <param name="vote">The vote to operate on.</param>
    internal static void SetResultLabel(PdfVote vote)
    {
        var endResults = vote.EndResult?.BallotEndResults;
        if (endResults == null)
        {
            return;
        }

        foreach (var ballotResult in endResults)
        {
            var label = 1 << (ballotResult.QuestionEndResults.Count - 1);
            var currentQuestionAcceptedBit = (int)PdfBallotEndResultLabel.Question1Accepted;
            foreach (var questionEndResult in ballotResult.QuestionEndResults)
            {
                if (questionEndResult.Accepted)
                {
                    label |= currentQuestionAcceptedBit;
                }

                currentQuestionAcceptedBit <<= 1;
            }

            ballotResult.QuestionEndResultLabel = (PdfBallotEndResultLabel)label;
        }
    }

    private static void SetLabels(PdfVoteBallotDomainOfInfluenceResult ballotDoiResult)
    {
        SetLabels(ballotDoiResult, x => x.Ballot?.BallotQuestions, x => x.Ballot?.TieBreakQuestions);

        foreach (var result in ballotDoiResult.Results)
        {
            SetLabels(result);
        }

        if (ballotDoiResult.NotAssignableResult != null)
        {
            SetLabels(ballotDoiResult.NotAssignableResult);
        }

        if (ballotDoiResult.AggregatedResult != null)
        {
            foreach (var result in ballotDoiResult.AggregatedResult.Results)
            {
                SetLabels(result);
            }

            SetLabels(
                ballotDoiResult.AggregatedResult,
                x => x.QuestionResults?.Select(r => r.Question!),
                x => x.TieBreakQuestionResults?.Select(r => r.Question!));
        }
    }

    private static void SetLabels(PdfVoteDomainOfInfluenceBallotResult ballotDoiResult)
    {
        SetLabels(
            ballotDoiResult,
            x => x.QuestionResults?.Select(r => r.Question!),
            x => x.TieBreakQuestionResults?.Select(r => r.Question!));

        if (ballotDoiResult.Results == null)
        {
            return;
        }

        foreach (var ballotCcResult in ballotDoiResult.Results)
        {
            SetLabels(
                ballotCcResult,
                x => x.QuestionResults?.Select(r => r.Question!),
                x => x.TieBreakQuestionResults?.Select(r => r.Question!));
        }
    }

    private static void SetLabels(PdfVoteCountingCircleBallotResult ballotDoiResult)
    {
        SetLabels(
            ballotDoiResult,
            x => x.QuestionResults?.Select(r => r.Question!),
            x => x.TieBreakQuestionResults?.Select(r => r.Question!));
    }
}
