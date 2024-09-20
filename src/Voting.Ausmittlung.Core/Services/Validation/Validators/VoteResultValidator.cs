// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class VoteResultValidator : CountingCircleResultValidator<VoteResult>
{
    private readonly IValidator<BallotResult> _ballotResultValidator;

    public VoteResultValidator(
        IValidator<ContestCountingCircleDetails> ccDetailsValidator,
        IValidator<BallotResult> ballotResultValidator)
        : base(ccDetailsValidator)
    {
        _ballotResultValidator = ballotResultValidator;
    }

    public override IEnumerable<ValidationResult> Validate(VoteResult data, ValidationContext context)
    {
        foreach (var result in base.Validate(data, context))
        {
            yield return result;
        }

        foreach (var ballotResult in data.Results)
        {
            foreach (var result in _ballotResultValidator.Validate(ballotResult, context))
            {
                yield return result;
            }
        }
    }
}
