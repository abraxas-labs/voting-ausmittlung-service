// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using SharedProto = Abraxas.Voting.Ausmittlung.Shared.V1;

namespace Voting.Ausmittlung.Core.Services.Validation.Validators;

public class MajorityElectionCandidateResultValidator : IValidator<MajorityElectionCandidateResultBase>
{
    public IEnumerable<ValidationResult> Validate(MajorityElectionCandidateResultBase data, ValidationContext context)
    {
        yield return ValidateAccountedBallotsGreaterOrEqualCandidateVotes(data, context.CountOfVoters);
    }

    private ValidationResult ValidateAccountedBallotsGreaterOrEqualCandidateVotes(
        MajorityElectionCandidateResultBase candidateResult,
        PoliticalBusinessNullableCountOfVoters countOfVoters)
    {
        return new ValidationResult(
            SharedProto.Validation.MajorityElectionAccountedBallotsGreaterOrEqualCandidateVotes,
            countOfVoters.ConventionalAccountedBallots.GetValueOrDefault() >= candidateResult.ConventionalVoteCount.GetValueOrDefault());
    }
}
