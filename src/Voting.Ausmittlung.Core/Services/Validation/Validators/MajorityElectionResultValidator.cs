// (c) Copyright 2022 by Abraxas Informatik AG
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

    public MajorityElectionResultValidator(
        IValidator<PoliticalBusinessNullableCountOfVoters> countOfVotersValidator,
        IValidator<MajorityElectionCandidateResultBase> candidateValidator,
        IValidator<ContestCountingCircleDetails> ccDetailsValidator)
        : base(ccDetailsValidator)
    {
        _countOfVotersValidator = countOfVotersValidator;
        _candidateValidator = candidateValidator;
    }

    public override IEnumerable<ValidationResult> Validate(MajorityElectionResult data, ValidationContext context)
    {
        foreach (var result in base.Validate(data, context))
        {
            yield return result;
        }

        if (context.CurrentContestCountingCircleDetails.EVoting)
        {
            yield return new ValidationResult(
                SharedProto.Validation.EVotingWriteInsMapped,
                !data.HasUnmappedWriteIns);
        }

        foreach (var result in _countOfVotersValidator.Validate(data.CountOfVoters, context))
        {
            yield return result;
        }

        yield return ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes(data);

        if (context.IsDetailedEntry)
        {
            yield return ValidateBundlesNotInProcess(data);
        }
        else
        {
            yield return ValidateCandidateVotesNotNull(data);
            if (data.MajorityElection.NumberOfMandates != 1)
            {
                yield return ValidateEmptyVoteCountNotNull(data);
            }

            if (data.MajorityElection.InvalidVotes && data.MajorityElection.NumberOfMandates != 1)
            {
                yield return ValidateInvalidVoteCountNotNull(data);
            }
        }

        foreach (var result in ValidateCandidateResults(data, context))
        {
            yield return result;
        }
    }

    private IEnumerable<ValidationResult> ValidateCandidateResults(MajorityElectionResult electionResult, ValidationContext context)
    {
        yield return electionResult.CandidateResults
                        .Cast<MajorityElectionCandidateResultBase>()
                        .Concat(electionResult.SecondaryMajorityElectionResults.SelectMany(x => x.CandidateResults))
                        .SelectMany(x => _candidateValidator.Validate(x, context))
                        .FirstInvalidOrElseFirstValid();
    }

    private ValidationResult ValidateCandidateVotesNotNull(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionCandidateVotesNotNull,
            electionResult.CandidateResults.All(x => x.ConventionalVoteCount.HasValue) && electionResult.ConventionalSubTotal.IndividualVoteCount.HasValue);
    }

    private ValidationResult ValidateEmptyVoteCountNotNull(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull,
            electionResult.ConventionalSubTotal.EmptyVoteCount.HasValue);
    }

    private ValidationResult ValidateInvalidVoteCountNotNull(MajorityElectionResult electionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull,
            electionResult.ConventionalSubTotal.InvalidVoteCount.HasValue);
    }

    private ValidationResult ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes(MajorityElectionResult electionResult)
    {
        var totalAccountedBallots = electionResult.CountOfVoters.ConventionalAccountedBallots.GetValueOrDefault();
        var numberOfMandates = electionResult.MajorityElection.NumberOfMandates;
        var emptyVoteCount = electionResult.ConventionalSubTotal.EmptyVoteCount.GetValueOrDefault();
        var invalidVoteCount = electionResult.ConventionalSubTotal.InvalidVoteCount.GetValueOrDefault();
        var candidateVoteCountInclIndividual = electionResult.ConventionalSubTotal.TotalCandidateVoteCountInclIndividual;

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
