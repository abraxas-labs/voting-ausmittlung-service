// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Utils.Models;

public class VoteDomainOfInfluenceBallotResult : DomainOfInfluenceResult
{
    private readonly List<VoteCountingCircleBallotResult> _results = new();

    public VoteDomainOfInfluenceBallotResult(Ballot ballot, DomainOfInfluence? doi)
    {
        DomainOfInfluence = doi;
        Ballot = ballot;
        QuestionResultsByQuestionId = ballot.BallotQuestions.ToDictionary(
            q => q.Id,
            x => new BallotQuestionDomainOfInfluenceResult
            {
                Question = x,
                QuestionId = x.Id,
            });
        TieBreakQuestionResultsByQuestionId = ballot.TieBreakQuestions.ToDictionary(
            q => q.Id,
            x => new TieBreakQuestionDomainOfInfluenceResult
            {
                Question = x,
                QuestionId = x.Id,
            });
    }

    public Ballot Ballot { get; }

    public IReadOnlyCollection<VoteCountingCircleBallotResult> Results => _results;

    public IEnumerable<BallotQuestionDomainOfInfluenceResult> QuestionResults => QuestionResultsByQuestionId.Values
        .OrderBy(x => x.Question.Number);

    public IEnumerable<TieBreakQuestionDomainOfInfluenceResult> TieBreakQuestionResults => TieBreakQuestionResultsByQuestionId.Values
        .OrderBy(x => x.Question.Number);

    internal IReadOnlyDictionary<Guid, BallotQuestionDomainOfInfluenceResult> QuestionResultsByQuestionId { get; }

    internal IReadOnlyDictionary<Guid, TieBreakQuestionDomainOfInfluenceResult> TieBreakQuestionResultsByQuestionId { get; }

    public void AddResult(BallotResult result) => _results.Add(new VoteCountingCircleBallotResult(result));
}
