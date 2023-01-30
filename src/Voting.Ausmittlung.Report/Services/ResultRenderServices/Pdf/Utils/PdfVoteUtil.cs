// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils;

public static class PdfVoteUtil
{
    private const int TieBreakQuestionCountWith2Questions = 1;
    private const int TieBreakQuestionCountWith3Questions = 3;

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
                questions[0].Label = PdfVoteQuestionLabel.QuestionCount1Q1;
                break;

            case > 1 when tieBreakQuestions.Count == 0:
                throw new InvalidOperationException("tie break questions must not be empty when the ballot has more than 1 question");

            case 2 when tieBreakQuestions.Count != 0 && tieBreakQuestions.Count != TieBreakQuestionCountWith2Questions:
                throw new InvalidOperationException("ballots with 2 questions must have 0 or 1 tie break question");

            case 2:
                questions[0].Label = PdfVoteQuestionLabel.QuestionCount2Q1;
                questions[1].Label = PdfVoteQuestionLabel.QuestionCount2Q2;

                if (tieBreakQuestions.Count == TieBreakQuestionCountWith2Questions)
                {
                    tieBreakQuestions[0].Label = PdfVoteQuestionLabel.QuestionCount2TBQ1;
                }

                break;

            case 3 when tieBreakQuestions.Count != 0 && tieBreakQuestions.Count != TieBreakQuestionCountWith3Questions:
                throw new InvalidOperationException("ballots with 3 questions must have 0 or 3 tie break questions");

            case 3:
                questions[0].Label = PdfVoteQuestionLabel.QuestionCount3Q1;
                questions[1].Label = PdfVoteQuestionLabel.QuestionCount3Q2;
                questions[2].Label = PdfVoteQuestionLabel.QuestionCount3Q3;

                if (tieBreakQuestions.Count == TieBreakQuestionCountWith3Questions)
                {
                    tieBreakQuestions[0].Label = PdfVoteQuestionLabel.QuestionCount3TBQ1;
                    tieBreakQuestions[1].Label = PdfVoteQuestionLabel.QuestionCount3TBQ2;
                    tieBreakQuestions[2].Label = PdfVoteQuestionLabel.QuestionCount3TBQ3;
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
}
