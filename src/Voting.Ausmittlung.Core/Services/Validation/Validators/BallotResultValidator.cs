// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class BallotResultValidator : IValidator<BallotResult>
{
    private readonly IValidator<PoliticalBusinessNullableCountOfVoters> _countOfVotersValidator;
    private readonly IValidator<BallotQuestionResult> _questionResultValidator;
    private readonly IValidator<TieBreakQuestionResult> _tieBreakQuestionResultValidator;

    public BallotResultValidator(
        IValidator<PoliticalBusinessNullableCountOfVoters> countOfVotersValidator,
        IValidator<BallotQuestionResult> questionResultValidator,
        IValidator<TieBreakQuestionResult> tieBreakQuestionResultValidator)
    {
        _countOfVotersValidator = countOfVotersValidator;
        _questionResultValidator = questionResultValidator;
        _tieBreakQuestionResultValidator = tieBreakQuestionResultValidator;
    }

    public IEnumerable<ValidationResult> Validate(BallotResult data, ValidationContext context)
    {
        data.OrderQuestionResultsAndSubTotals();

        var results = new List<ValidationResult>();

        results.AddRange(_countOfVotersValidator.Validate(data.CountOfVoters, context));

        if (context.IsDetailedEntry)
        {
            results.Add(ValidateBundlesNotInProcess(data));
        }

        results.AddRange(ValidateQuestionResults(data, context));

        return results;
    }

    private IEnumerable<ValidationResult> ValidateQuestionResults(BallotResult data, ValidationContext context)
    {
        if (data.QuestionResults.Count > 3)
        {
            throw new InvalidOperationException($"ballots with {data.QuestionResults.Count} questions are not supported yet.");
        }

        var results = new List<ValidationResult>();

        foreach (var questionResult in data.QuestionResults)
        {
            results.AddRange(_questionResultValidator.Validate(questionResult, context));
        }

        foreach (var tieBreakQuestionResult in data.TieBreakQuestionResults)
        {
            results.AddRange(_tieBreakQuestionResultValidator.Validate(tieBreakQuestionResult, context));
        }

        return results;
    }

    private ValidationResult ValidateBundlesNotInProcess(BallotResult result)
    {
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessBundlesNotInProcess,
            result.AllBundlesReviewedOrDeleted);
    }
}
