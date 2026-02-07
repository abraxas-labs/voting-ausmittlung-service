// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class SecondaryMajorityElectionResultValidator : IValidator<SecondaryMajorityElectionResult>
{
    private readonly IValidator<MajorityElectionCandidateResultBase> _candidateValidator;

    public SecondaryMajorityElectionResultValidator(IValidator<MajorityElectionCandidateResultBase> candidateValidator)
    {
        _candidateValidator = candidateValidator;
    }

    public IEnumerable<ValidationResult> Validate(SecondaryMajorityElectionResult data, ValidationContext context)
    {
        var results = new List<ValidationResult>
        {
            ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes(data),
        };

        if (!context.IsDetailedEntry)
        {
            results.Add(ValidateCandidateVotesNotNull(data));

            if (data.SecondaryMajorityElection.NumberOfMandates != 1)
            {
                results.Add(ValidateEmptyVoteCountNotNull(data));
            }

            if (data.PrimaryResult.MajorityElection.Contest.CantonDefaults.MajorityElectionInvalidVotes)
            {
                results.Add(ValidateInvalidVoteCountNotNull(data));
            }
        }

        results.AddRange(ValidateCandidateResults(data, context));

        foreach (var result in results)
        {
            result.ValidationGroup = SharedProto.ValidationGroup.SecondaryMajorityElection;
            result.GroupValue = data.SecondaryMajorityElection.Title;
        }

        return results;
    }

    private IEnumerable<ValidationResult> ValidateCandidateResults(SecondaryMajorityElectionResult secondaryElectionResult, ValidationContext context)
    {
        // An election can have temporarily no official candidates (ex: unpopular mandate).
        if (secondaryElectionResult.CandidateResults.Count == 0)
        {
            if (context.TestingPhaseEnded)
            {
                yield return new ValidationResult(SharedProto.Validation.MajorityElectionHasCandidates, false);
            }

            yield break;
        }

        yield return secondaryElectionResult.CandidateResults
            .SelectMany(x => _candidateValidator.Validate(x, context))
            .FirstInvalidOrElseFirstValid();
        yield return ValidateCandidateResultVoteCount(secondaryElectionResult);
    }

    private ValidationResult ValidateCandidateResultVoteCount(SecondaryMajorityElectionResult secondaryElectionResult)
    {
        var primaryVoteCounts = secondaryElectionResult.PrimaryResult.CandidateResults
            .ToDictionary(x => x.CandidateId, x => x.VoteCount);
        return secondaryElectionResult.CandidateResults
            .Select(c => new ValidationResult(SharedProto.Validation.MajorityElectionSecondaryReferencedCandidateMoreVotesThanPrimary, !c.Candidate.CandidateReferenceId.HasValue || c.VoteCount <= primaryVoteCounts[c.Candidate.CandidateReferenceId.Value]))
            .FirstInvalidOrElseFirstValid();
    }

    private ValidationResult ValidateCandidateVotesNotNull(SecondaryMajorityElectionResult secondaryElectionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionCandidateVotesNotNull,
            secondaryElectionResult.CandidateResults.All(x => x.ConventionalVoteCount.HasValue) && (secondaryElectionResult.SecondaryMajorityElection.IndividualCandidatesDisabled || secondaryElectionResult.ConventionalSubTotal.IndividualVoteCount.HasValue));
    }

    private ValidationResult ValidateEmptyVoteCountNotNull(SecondaryMajorityElectionResult secondaryElectionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionEmptyVoteCountNotNull,
            secondaryElectionResult.ConventionalSubTotal.EmptyVoteCountExclWriteIns.HasValue);
    }

    private ValidationResult ValidateInvalidVoteCountNotNull(SecondaryMajorityElectionResult secondaryElectionResult)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionInvalidVoteCountNotNull,
            secondaryElectionResult.ConventionalSubTotal.InvalidVoteCount.HasValue);
    }

    private ValidationResult ValidateNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotes(SecondaryMajorityElectionResult secondaryElectionResult)
    {
        var totalAccountedBallots = secondaryElectionResult.PrimaryResult.CountOfVoters.TotalAccountedBallots;
        var numberOfMandates = secondaryElectionResult.SecondaryMajorityElection.NumberOfMandates;
        var emptyVoteCount = secondaryElectionResult.EmptyVoteCount;
        var invalidVoteCount = secondaryElectionResult.InvalidVoteCount;
        var candidateVoteCountInclIndividual = secondaryElectionResult.TotalCandidateVoteCountInclIndividual;

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
}
