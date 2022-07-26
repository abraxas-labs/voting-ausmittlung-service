﻿// (c) Copyright 2022 by Abraxas Informatik AG
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
    private readonly ContestCountingCircleDetailsValidationResultsBuilder _ccDetailsValidationResultsBuilder;
    private readonly VoteResultValidationResultsBuilder _voteResultValidationResultsBuilder;
    private readonly ProportionalElectionResultValidationResultsBuilder _proportionalElectionResultValidationResultsBuilder;
    private readonly MajorityElectionResultValidationResultsBuilder _majorityElectionResultValidationResultsBuilder;
    private readonly IValidationResultsEnsurerUtils _validationResultsEnsurerUtils;

    public ValidationResultsEnsurer(
        ContestCountingCircleDetailsValidationResultsBuilder ccDetailsValidationResultsBuilder,
        VoteResultValidationResultsBuilder voteResultValidationResultsBuilder,
        ProportionalElectionResultValidationResultsBuilder proportionalElectionResultValidationResultsBuilder,
        MajorityElectionResultValidationResultsBuilder majorityElectionResultValidationResultsBuilder,
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

    private void EnsureIsValid(List<ValidationResult> validationResults)
    {
        _validationResultsEnsurerUtils.EnsureIsValid(validationResults);
    }
}
