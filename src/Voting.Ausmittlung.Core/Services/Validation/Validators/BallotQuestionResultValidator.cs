// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class BallotQuestionResultValidator : IValidator<BallotQuestionResult>
{
    public IEnumerable<ValidationResult> Validate(BallotQuestionResult data, ValidationContext context)
    {
        var isStandardBallot = data.BallotResult.Ballot.BallotType == BallotType.StandardBallot;

        if (!context.IsDetailedEntry)
        {
            yield return isStandardBallot
                ? ValidateCountOfAnswerNotNull(data)
                : ValidateQnCountOfAnswerNotNull(data);
        }

        yield return isStandardBallot
            ? ValidateAccountedBallotsEqualCountOfAnswer(data, data.BallotResult.CountOfVoters)
            : ValidateAccountedBallotsEqualQnCountOfAnswer(data, data.BallotResult.CountOfVoters);
    }

    private ValidationResult ValidateCountOfAnswerNotNull(BallotQuestionResult questionResult)
    {
        var subTotal = questionResult.ConventionalSubTotal;

        return new ValidationResult(
            SharedProto.Validation.VoteCountOfAnswerNotNull,
            subTotal.TotalCountOfAnswerYes.HasValue && subTotal.TotalCountOfAnswerNo.HasValue);
    }

    private ValidationResult ValidateQnCountOfAnswerNotNull(BallotQuestionResult questionResult)
    {
        var subTotal = questionResult.ConventionalSubTotal;

        return new ValidationResult(
            SharedProto.Validation.VoteQnCountOfAnswerNotNull,
            subTotal.TotalCountOfAnswerYes.HasValue && subTotal.TotalCountOfAnswerNo.HasValue && subTotal.TotalCountOfAnswerUnspecified.HasValue,
            new ValidationVoteAccountedBallotsEqualQnData
            {
                QuestionNumber = questionResult.Question.Number,
            });
    }

    private ValidationResult ValidateAccountedBallotsEqualCountOfAnswer(BallotQuestionResult questionResult, PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        return new ValidationResult(
            SharedProto.Validation.VoteAccountedBallotsEqualCountOfAnswer,
            countOfVoters.ConventionalAccountedBallots == questionResult.ConventionalSubTotal.CountOfAnswerTotal);
    }

    private ValidationResult ValidateAccountedBallotsEqualQnCountOfAnswer(BallotQuestionResult questionResult, PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        return new ValidationResult(
            SharedProto.Validation.VoteAccountedBallotsEqualQnCountOfAnswer,
            countOfVoters.ConventionalAccountedBallots == questionResult.ConventionalSubTotal.CountOfAnswerTotal,
            new ValidationVoteAccountedBallotsEqualQnData
            {
                QuestionNumber = questionResult.Question.Number,
            });
    }
}
