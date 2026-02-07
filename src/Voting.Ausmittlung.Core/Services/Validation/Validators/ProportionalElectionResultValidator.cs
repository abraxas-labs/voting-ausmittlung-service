// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
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
        yield return ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRows(data);
        yield return ValidateBundlesNotInProcess(data);
    }

    private ValidationResult ValidateAccountedBallotsEqualModifiedPlusUnmodifiedLists(ProportionalElectionResult electionResult)
    {
        var totalAccountedBallots = electionResult.CountOfVoters.TotalAccountedBallots;
        var totalCountOfBallots = electionResult.TotalCountOfBallots;
        var totalCountOfUnmodifiedLists = electionResult.TotalCountOfUnmodifiedLists;
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

    private ValidationResult ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRows(ProportionalElectionResult electionResult)
    {
        var totalAccountedBallots = electionResult.CountOfVoters.TotalAccountedBallots;
        var numberOfMandates = electionResult.ProportionalElection.NumberOfMandates;
        var blankRowsCount = electionResult.ListResults.Sum(x => x.BlankRowsCount) + electionResult.TotalCountOfBlankRowsOnListsWithoutParty;
        var candidateVoteCount = electionResult.ListResults.Sum(l => l.ListVotesCount);

        var result1 = candidateVoteCount + blankRowsCount;
        var result2 = numberOfMandates * totalAccountedBallots;

        return new ValidationResult(
            SharedProto.Validation.ProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRows,
            result1 == result2,
            new ValidationProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData
            {
                BlankRowsCount = blankRowsCount,
                CandidateVotes = candidateVoteCount,
                NumberOfMandates = numberOfMandates,
                TotalAccountedBallots = totalAccountedBallots,
                SumVoteCount = result1,
                NumberOfMandatesTimesAccountedBallots = result2,
            });
    }

    private ValidationResult ValidateBundlesNotInProcess(ProportionalElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessBundlesNotInProcess,
            electionResult.AllBundlesReviewedOrDeleted);
    }
}
