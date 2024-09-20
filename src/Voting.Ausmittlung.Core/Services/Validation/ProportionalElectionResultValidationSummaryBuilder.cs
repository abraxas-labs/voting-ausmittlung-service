// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Repositories;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class ProportionalElectionResultValidationSummaryBuilder : CountingCircleResultValidationSummaryBuilder<DataModels.ProportionalElectionResult>
{
    private readonly ProportionalElectionResultRepo _proportionalElectionResultRepository;
    private readonly IValidator<DataModels.ProportionalElectionResult> _proportionalElectionResultValidator;

    public ProportionalElectionResultValidationSummaryBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        ProportionalElectionResultRepo proportionalElectionResultRepository,
        IMapper mapper,
        PermissionService permissionService,
        IValidator<DataModels.ProportionalElectionResult> proportionalElectionResultValidator)
        : base(contestRepo, contestCountingCircleDetailsRepo, mapper, permissionService)
    {
        _proportionalElectionResultRepository = proportionalElectionResultRepository;
        _proportionalElectionResultValidator = proportionalElectionResultValidator;
    }

    public async Task<ValidationSummary> BuildEnterCountOfVotersValidationSummary(Guid electionResultId, PoliticalBusinessCountOfVoters countOfVoters)
    {
        var electionResult = await GetElectionResult(electionResultId);
        Mapper.Map(countOfVoters, electionResult.CountOfVoters);
        return new ValidationSummary(await BuildValidationResults(electionResult));
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(DataModels.ProportionalElectionResult electionResult)
    {
        var ccDetails = await GetContestCountingCircleDetails(electionResult.ProportionalElection.ContestId, electionResult.CountingCircle.BasisCountingCircleId);
        return BuildValidationResults(electionResult, ccDetails);
    }

    internal List<ValidationResult> BuildValidationResults(DataModels.ProportionalElectionResult electionResult, DataModels.ContestCountingCircleDetails ccDetails)
    {
        var context = BuildValidationContext(
            ccDetails,
            DataModels.PoliticalBusinessType.ProportionalElection,
            true,
            electionResult.ProportionalElection.DomainOfInfluence.Type);

        context.CountOfVoters = electionResult.CountOfVoters;
        return _proportionalElectionResultValidator.Validate(electionResult, context).ToList();
    }

    private async Task<DataModels.ProportionalElectionResult> GetElectionResult(Guid electionId)
    {
        var electionResult = (await _proportionalElectionResultRepository.ListWithValidationContextData(x => x.Id == electionId, true))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException(electionId);

        EnsureValidationPermissions(electionResult);
        return electionResult;
    }
}
