// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class ProportionalElectionResultValidator : CountingCircleResultValidator<ProportionalElectionResult>
{
    private readonly IValidator<PoliticalBusinessNullableCountOfVoters> _countOfVotersValidator;

    public ProportionalElectionResultValidator(
        IValidator<PoliticalBusinessNullableCountOfVoters> countOfVotersValidator,
        IValidator<ContestCountingCircleDetails> ccDetailsValidator)
        : base(ccDetailsValidator)
    {
        _countOfVotersValidator = countOfVotersValidator;
    }

    public override IEnumerable<ValidationResult> Validate(ProportionalElectionResult data, ValidationContext context)
    {
        foreach (var result in base.Validate(data, context))
        {
            yield return result;
        }

        foreach (var result in _countOfVotersValidator.Validate(data.CountOfVoters, context))
        {
            yield return result;
        }

        yield return ValidateAccountedBallotsEqualModifiedPlusUnmodifiedLists(data);
        yield return ValidateBundlesNotInProcess(data);
    }

    private ValidationResult ValidateAccountedBallotsEqualModifiedPlusUnmodifiedLists(ProportionalElectionResult electionResult)
    {
        var totalAccountedBallots = electionResult.CountOfVoters.ConventionalAccountedBallots.GetValueOrDefault();
        var totalCountOfBallots = electionResult.ConventionalSubTotal.TotalCountOfBallots;
        var totalCountOfUnmodifiedLists = electionResult.ConventionalSubTotal.TotalCountOfUnmodifiedLists;
        var sumBallotsAndUnmodifiedLists = totalCountOfBallots + totalCountOfUnmodifiedLists;

        return new ValidationResult(
            SharedProto.Validation.ProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedLists,
            totalAccountedBallots == sumBallotsAndUnmodifiedLists,
            new ValidationProportionalElectionAccountedBallotsEqualModifiedPlusUnmodifiedListsData
            {
                TotalAccountedBallots = totalAccountedBallots,
                TotalCountOfBallots = totalCountOfBallots,
                TotalCountOfUnmodifiedLists = totalCountOfUnmodifiedLists,
                SumBallotsAndUnmodifiedLists = sumBallotsAndUnmodifiedLists,
            });
    }

    private ValidationResult ValidateBundlesNotInProcess(ProportionalElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessBundlesNotInProcess,
            electionResult.AllBundlesReviewedOrDeleted);
    }
}
