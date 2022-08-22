// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class ProportionalElectionResultValidationResultsBuilder : CountingCircleResultValidationResultsBuilder<DataModels.ProportionalElectionResult>
{
    private readonly IDbRepository<DataContext, DataModels.ProportionalElectionResult> _proportionalElectionResultRepository;
    private readonly IValidator<DataModels.ProportionalElectionResult> _proportionalElectionResultValidator;

    public ProportionalElectionResultValidationResultsBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        IDbRepository<DataContext, DataModels.ProportionalElectionResult> proportionalElectionResultRepository,
        IMapper mapper,
        PermissionService permissionService,
        IValidator<DataModels.ProportionalElectionResult> proportionalElectionResultValidator)
        : base(contestRepo, contestCountingCircleDetailsRepo, mapper, permissionService)
    {
        _proportionalElectionResultRepository = proportionalElectionResultRepository;
        _proportionalElectionResultValidator = proportionalElectionResultValidator;
    }

    public async Task<List<ValidationResult>> BuildEnterCountOfVotersValidationResults(Guid electionResultId, PoliticalBusinessCountOfVoters countOfVoters)
    {
        var electionResult = await GetElectionResult(electionResultId);
        Mapper.Map(countOfVoters, electionResult.CountOfVoters);
        return await BuildValidationResults(electionResult);
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(DataModels.ProportionalElectionResult electionResult)
    {
        var context = await BuildValidationContext(
            electionResult.ProportionalElection.ContestId,
            electionResult.CountingCircle.BasisCountingCircleId,
            DataModels.PoliticalBusinessType.ProportionalElection,
            true,
            electionResult.ProportionalElection.DomainOfInfluence.Type);

        context.CountOfVoters = electionResult.CountOfVoters;

        return _proportionalElectionResultValidator.Validate(electionResult, context).ToList();
    }

    private async Task<DataModels.ProportionalElectionResult> GetElectionResult(Guid electionId)
    {
        var electionResult = await _proportionalElectionResultRepository.Query()
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Include(x => x.CountingCircle.ResponsibleAuthority)
            .FirstOrDefaultAsync(x => x.Id == electionId)
            ?? throw new EntityNotFoundException(electionId);

        EnsureValidationPermissions(electionResult);
        return electionResult;
    }
}
