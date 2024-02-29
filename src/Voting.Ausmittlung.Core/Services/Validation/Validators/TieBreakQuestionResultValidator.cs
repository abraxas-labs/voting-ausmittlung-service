// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class TieBreakQuestionResultValidator : IValidator<TieBreakQuestionResult>
{
    public IEnumerable<ValidationResult> Validate(TieBreakQuestionResult data, ValidationContext context)
    {
        if (!context.IsDetailedEntry)
        {
            yield return ValidateTieBreakQnCountOfAnswerNotNull(data);
        }

        yield return ValidateAccountedBallotsEqualTieBreakQnCountOfAnswer(data, data.BallotResult.CountOfVoters);
    }

    private ValidationResult ValidateTieBreakQnCountOfAnswerNotNull(TieBreakQuestionResult questionResult)
    {
        var subTotal = questionResult.ConventionalSubTotal;

        return new ValidationResult(
            SharedProto.Validation.VoteTieBreakQnCountOfAnswerNotNull,
            subTotal.TotalCountOfAnswerQ1.HasValue && subTotal.TotalCountOfAnswerQ2.HasValue && subTotal.TotalCountOfAnswerUnspecified.HasValue,
            new ValidationVoteAccountedBallotsEqualQnData
            {
                QuestionNumber = questionResult.Question.Number,
            });
    }

    private ValidationResult ValidateAccountedBallotsEqualTieBreakQnCountOfAnswer(TieBreakQuestionResult questionResult, PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        return new ValidationResult(
            SharedProto.Validation.VoteAccountedBallotsEqualTieBreakQnCountOfAnswer,
            countOfVoters.ConventionalAccountedBallots.GetValueOrDefault() == questionResult.ConventionalSubTotal.CountOfAnswerTotal,
            new ValidationVoteAccountedBallotsEqualQnData
            {
                QuestionNumber = questionResult.Question.Number,
            });
    }
}
