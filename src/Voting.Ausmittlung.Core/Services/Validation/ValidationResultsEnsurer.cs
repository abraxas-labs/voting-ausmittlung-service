// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Threading.Tasks;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Utils;
using Voting.Ausmittlung.Data.Models;
using ContestCountingCircleDetails = Voting.Ausmittlung.Core.Domain.ContestCountingCircleDetails;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class ValidationResultsEnsurer
{
    private readonly ContestCountingCircleDetailsValidationSummaryBuilder _ccDetailsValidationResultsBuilder;
    private readonly VoteResultValidationSummaryBuilder _voteResultValidationResultsBuilder;
    private readonly ProportionalElectionResultValidationSummaryBuilder _proportionalElectionResultValidationResultsBuilder;
    private readonly MajorityElectionResultValidationSummaryBuilder _majorityElectionResultValidationResultsBuilder;
    private readonly IValidationResultsEnsurerUtils _validationResultsEnsurerUtils;

    public ValidationResultsEnsurer(
        ContestCountingCircleDetailsValidationSummaryBuilder ccDetailsValidationResultsBuilder,
        VoteResultValidationSummaryBuilder voteResultValidationResultsBuilder,
        ProportionalElectionResultValidationSummaryBuilder proportionalElectionResultValidationResultsBuilder,
        MajorityElectionResultValidationSummaryBuilder majorityElectionResultValidationResultsBuilder,
        IValidationResultsEnsurerUtils validationResultsEnsurerUtils)
    {
        _ccDetailsValidationResultsBuilder = ccDetailsValidationResultsBuilder;
        _voteResultValidationResultsBuilder = voteResultValidationResultsBuilder;
        _proportionalElectionResultValidationResultsBuilder = proportionalElectionResultValidationResultsBuilder;
        _majorityElectionResultValidationResultsBuilder = majorityElectionResultValidationResultsBuilder;
        _validationResultsEnsurerUtils = validationResultsEnsurerUtils;
    }

    internal async Task EnsureContestCountingCircleDetailsIsValid(ContestCountingCircleDetails domainDetails, Data.Models.ContestCountingCircleDetails ccDetails)
    {
        EnsureIsValid(await _ccDetailsValidationResultsBuilder.BuildValidationResults(domainDetails, ccDetails));
    }

    internal async Task EnsureVoteResultIsValid(VoteResult voteResult)
    {
        EnsureIsValid(await _voteResultValidationResultsBuilder.BuildValidationResults(voteResult));
    }

    internal async Task EnsureProportionalElectionResultIsValid(ProportionalElectionResult electionResult)
    {
        EnsureIsValid(await _proportionalElectionResultValidationResultsBuilder.BuildValidationResults(electionResult));
    }

    internal async Task EnsureMajorityElectionResultIsValid(MajorityElectionResult electionResult)
    {
        EnsureIsValid(await _majorityElectionResultValidationResultsBuilder.BuildValidationResults(electionResult));
    }

    internal void EnsureIsValid(IReadOnlyCollection<ValidationResult> validationResults)
    {
        _validationResultsEnsurerUtils.EnsureIsValid(validationResults);
    }
}
