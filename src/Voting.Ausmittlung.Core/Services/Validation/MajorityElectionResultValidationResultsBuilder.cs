// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class MajorityElectionResultValidationResultsBuilder : CountingCircleResultValidationResultsBuilder<DataModels.MajorityElectionResult>
{
    private readonly IDbRepository<DataContext, DataModels.MajorityElectionResult> _majorityElectionResultRepository;
    private readonly MajorityElectionResultBuilder _majorityElectionResultBuilder;
    private readonly IValidator<DataModels.MajorityElectionResult> _majorityElectionResultValidator;

    public MajorityElectionResultValidationResultsBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        IDbRepository<DataContext, DataModels.MajorityElectionResult> majorityElectionResultRepository,
        IMapper mapper,
        MajorityElectionResultBuilder majorityElectionResultBuilder,
        PermissionService permissionService,
        IValidator<DataModels.MajorityElectionResult> majorityElectionResultValidator)
        : base(contestRepo, contestCountingCircleDetailsRepo, mapper, permissionService)
    {
        _majorityElectionResultRepository = majorityElectionResultRepository;
        _majorityElectionResultBuilder = majorityElectionResultBuilder;
        _majorityElectionResultValidator = majorityElectionResultValidator;
    }

    public async Task<List<ValidationResult>> BuildEnterCountOfVotersValidationResults(Guid electionResultId, PoliticalBusinessCountOfVoters countOfVoters)
    {
        var electionResult = await GetElectionResult(electionResultId);
        Mapper.Map(countOfVoters, electionResult.CountOfVoters);
        return await BuildValidationResults(electionResult);
    }

    public async Task<List<ValidationResult>> BuildEnterCandidateResultsValidationResults(
        Guid electionResultId,
        PoliticalBusinessCountOfVoters countOfVoters,
        int? individualVoteCount,
        int? emptyVoteCount,
        int? invalidVoteCount,
        IReadOnlyCollection<MajorityElectionCandidateResult> candidateResults,
        IReadOnlyCollection<SecondaryMajorityElectionCandidateResults> secondaryCandidateResults)
    {
        var electionResult = await GetElectionResult(electionResultId);
        var candidateResultsEntered = new MajorityElectionCandidateResultsEntered
        {
            ElectionResultId = electionResultId.ToString(),
            IndividualVoteCount = individualVoteCount,
            EmptyVoteCount = emptyVoteCount,
            InvalidVoteCount = invalidVoteCount,
        };

        Mapper.Map(candidateResults, candidateResultsEntered.CandidateResults);
        Mapper.Map(secondaryCandidateResults, candidateResultsEntered.SecondaryElectionCandidateResults);

        _majorityElectionResultBuilder.UpdateConventionalResults(electionResult, candidateResultsEntered);

        Mapper.Map(countOfVoters, electionResult.CountOfVoters);

        return await BuildValidationResults(electionResult);
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(DataModels.MajorityElectionResult electionResult)
    {
        var context = await BuildValidationContext(
            electionResult.MajorityElection.ContestId,
            electionResult.CountingCircle.BasisCountingCircleId,
            DataModels.PoliticalBusinessType.MajorityElection,
            electionResult.Entry == DataModels.MajorityElectionResultEntry.Detailed,
            electionResult.MajorityElection.DomainOfInfluence.Type);

        context.CountOfVoters = electionResult.CountOfVoters;

        return _majorityElectionResultValidator.Validate(electionResult, context).ToList();
    }

    private async Task<DataModels.MajorityElectionResult> GetElectionResult(Guid electionResultId)
    {
        var electionResult = await _majorityElectionResultRepository.Query()
                .AsSplitQuery()
                .Include(x => x.CountingCircle.ResponsibleAuthority)
                .Include(x => x.MajorityElection.DomainOfInfluence)
                .Include(x => x.CandidateResults)
                .Include(x => x.SecondaryMajorityElectionResults).ThenInclude(x => x.CandidateResults)
                .Include(x => x.BallotGroupResults)
                .FirstOrDefaultAsync(x => x.Id == electionResultId)
            ?? throw new EntityNotFoundException(nameof(DataModels.MajorityElectionResult), electionResultId);

        EnsureValidationPermissions(electionResult);
        return electionResult;
    }
}
