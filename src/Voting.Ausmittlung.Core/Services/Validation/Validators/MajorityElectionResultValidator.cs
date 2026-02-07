// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class MajorityElectionResultValidator : CountingCircleResultValidator<MajorityElectionResult>
{
    private readonly IValidator<PoliticalBusinessNullableCountOfVoters> _countOfVotersValidator;
    private readonly IValidator<MajorityElectionCandidateResultBase> _candidateValidator;
    private readonly IValidator<SecondaryMajorityElectionResult> _secondaryMajorityElectionResultValidator;

    public MajorityElectionResultValidator(
        IValidator<PoliticalBusinessNullableCountOfVoters> countOfVotersValidator,
        IValidator<MajorityElectionCandidateResultBase> candidateValidator,
        IValidator<ContestCountingCircleDetails> ccDetailsValidator,
        IValidator<SecondaryMajorityElectionResult> secondaryMajorityElectionResultValidator)
        : base(ccDetailsValidator)
    {
        _countOfVotersValidator = countOfVotersValidator;
        _candidateValidator = candidateValidator;
        _secondaryMajorityElectionResultValidator = secondaryMajorityElectionResultValidator;
    }

    public override IEnumerable<ValidationResult> Validate(MajorityElectionResult data, ValidationContext context)
    {
        foreach (var result in base.Validate(data, context))
        {
            yield return result;
        }

        if (context.CurrentContestCountingCircleDetails.EVoting || context.CurrentContestCountingCircleDetails.ECountingResultsImported)
        {
            yield return new ValidationResult(
                SharedProto.Validation.WriteInsMapped,
                !data.HasUnmappedWriteIns);
        }

        foreach (var result in _countOfVotersValidator.Validate(data.CountOfVoters, context))
        {
            yield return result;
        }

        if (context.IsDetailedEntry)
        {
            yield return ValidateBundlesNotInProcess(data);
        }

        foreach (var result in ValidateMajorityElectionValidationGroup(data, context))
        {
            yield return result;
        }

        foreach (var result in data.SecondaryMajorityElectionResults
            .OrderBy(r => r.SecondaryMajorityElection.Title)
            .SelectMany(r => _secondaryMajorityElectionResultValidator.Validate(r, context)))
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateMajorityElectionValidationGroup(MajorityElectionResult data, ValidationContext context)
    {
        var results = new List<ValidationResult>
        {
            ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes(data),
        };

        if (!context.IsDetailedEntry)
        {
            results.Add(ValidateCandidateVotesNotNull(data));

            if (data.MajorityElection.NumberOfMandates != 1)
            {
                results.Add(ValidateEmptyVoteCountNotNull(data));
            }

            if (data.MajorityElection.Contest.CantonDefaults.MajorityElectionInvalidVotes)
            {
                results.Add(ValidateInvalidVoteCountNotNull(data));
            }
        }

        var candidateResultsValidationResult = ValidateCandidateResults(data, context);

        if (candidateResultsValidationResult != null)
        {
            results.Add(candidateResultsValidationResult);
        }

        foreach (var result in results)
        {
            result.ValidationGroup = SharedProto.ValidationGroup.MajorityElection;
            result.GroupValue = data.MajorityElection.Title;
        }

        return results;
    }

    private ValidationResult? ValidateCandidateResults(MajorityElectionResult electionResult, ValidationContext context)
    {
        // An election can have temporarily no official candidates (ex: unpopular mandate).
        if (electionResult.CandidateResults.Count == 0)
        {
            return context.TestingPhaseEnded
                ? new ValidationResult(SharedProto.Validation.MajorityElectionHasCandidates, false)
                : null;
        }

        return electionResult.CandidateResults
            .SelectMany(x => _candidateValidator.Validate(x, context))
            .FirstInvalidOrElseFirstValid();
    }

    private ValidationResult ValidateCandidateVotesNotNull(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionCandidateVotesNotNull,
            electionResult.CandidateResults.All(x => x.ConventionalVoteCount.HasValue) && (electionResult.MajorityElection.IndividualCandidatesDisabled || electionResult.ConventionalSubTotal.IndividualVoteCount.HasValue));
    }

    private ValidationResult ValidateEmptyVoteCountNotNull(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull,
            electionResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns.HasValue);
    }

    private ValidationResult ValidateInvalidVoteCountNotNull(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull,
            electionResult.ConventionalSubTotal.InvalidVoteCount.HasValue);
    }

    private ValidationResult ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes(MajorityElectionResult electionResult)
    {
        var totalAccountedBallots = electionResult.CountOfVoters.TotalAccountedBallots;
        var numberOfMandates = electionResult.MajorityElection.NumberOfMandates;
        var emptyVoteCount = electionResult.EmptyVoteCount;
        var invalidVoteCount = electionResult.InvalidVoteCount;
        var candidateVoteCountInclIndividual = electionResult.TotalCandidateVoteCountInclIndividual;

        var result1 = emptyVoteCount + invalidVoteCount + candidateVoteCountInclIndividual;
        var result2 = numberOfMandates * totalAccountedBallots;

        return new ValidationResult(
            SharedProto.Validation.MajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes,
            result1 == result2,
            new ValidationMajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotesData
            {
                EmptyVoteCount = emptyVoteCount,
                InvalidVoteCount = invalidVoteCount,
                CandidateVotesInclIndividual = candidateVoteCountInclIndividual,
                NumberOfMandates = numberOfMandates,
                TotalAccountedBallots = totalAccountedBallots,
                SumVoteCount = result1,
                NumberOfMandatesTimesAccountedBallots = result2,
            });
    }

    private ValidationResult ValidateBundlesNotInProcess(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.PoliticalBusinessBundlesNotInProcess,
            electionResult.AllBundlesReviewedOrDeleted);
    }
}
