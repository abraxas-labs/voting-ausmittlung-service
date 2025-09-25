// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Repositories;
using DataModels = Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class MajorityElectionResultValidationSummaryBuilder : CountingCircleResultValidationSummaryBuilder<DataModels.MajorityElectionResult>
{
    private readonly MajorityElectionResultRepo _majorityElectionResultRepository;
    private readonly MajorityElectionResultBuilder _majorityElectionResultBuilder;
    private readonly IValidator<DataModels.MajorityElectionResult> _majorityElectionResultValidator;

    public MajorityElectionResultValidationSummaryBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        MajorityElectionResultRepo majorityElectionResultRepository,
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

    public async Task<ValidationSummary> BuildEnterCountOfVotersValidationSummary(Guid electionResultId, PoliticalBusinessCountOfVoters countOfVoters)
    {
        var electionResult = await GetElectionResult(electionResultId);
        Mapper.Map(countOfVoters, electionResult.CountOfVoters);
        return new ValidationSummary(await BuildValidationResults(electionResult));
    }

    public async Task<ValidationSummary> BuildEnterCandidateResultsValidationSummary(
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

        return new ValidationSummary(await BuildValidationResults(electionResult));
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(DataModels.MajorityElectionResult electionResult)
    {
        var ccDetails = await GetContestCountingCircleDetails(electionResult.MajorityElection.ContestId, electionResult.CountingCircle.BasisCountingCircleId);
        return BuildValidationResults(electionResult, ccDetails);
    }

    internal List<ValidationResult> BuildValidationResults(DataModels.MajorityElectionResult electionResult, DataModels.ContestCountingCircleDetails ccDetails)
    {
        var context = BuildValidationContext(
            ccDetails,
            DataModels.PoliticalBusinessType.MajorityElection,
            electionResult.Entry == DataModels.MajorityElectionResultEntry.Detailed,
            electionResult.MajorityElection.DomainOfInfluence);

        context.CountOfVoters = electionResult.CountOfVoters;
        return _majorityElectionResultValidator.Validate(electionResult, context).ToList();
    }

    private async Task<DataModels.MajorityElectionResult> GetElectionResult(Guid electionResultId)
    {
        var electionResult = (await _majorityElectionResultRepository.ListWithValidationContextData(x => x.Id == electionResultId, true))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException(nameof(DataModels.MajorityElectionResult), electionResultId);

        EnsureValidationPermissions(electionResult);
        return electionResult;
    }
}
