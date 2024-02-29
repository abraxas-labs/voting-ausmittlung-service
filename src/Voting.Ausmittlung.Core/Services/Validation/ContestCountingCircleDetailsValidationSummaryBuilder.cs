// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using ValidationContext = Voting.Ausmittlung.Core.Services.Validation.Models.ValidationContext;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class ContestCountingCircleDetailsValidationSummaryBuilder
{
    private readonly IMapper _mapper;
    private readonly ContestCountingCircleDetailsRepo _ccDetailsRepository;
    private readonly ContestRepo _contestRepo;
    private readonly PermissionService _permissionService;
    private readonly IValidator<ContestCountingCircleDetails> _validator;

    public ContestCountingCircleDetailsValidationSummaryBuilder(
        IMapper mapper,
        ContestCountingCircleDetailsRepo ccDetailsRepository,
        ContestRepo contestRepo,
        PermissionService permissionService,
        IValidator<ContestCountingCircleDetails> validator)
    {
        _mapper = mapper;
        _ccDetailsRepository = ccDetailsRepository;
        _contestRepo = contestRepo;
        _permissionService = permissionService;
        _validator = validator;
    }

    public async Task<ValidationSummary> BuildUpdateContestCountingCircleDetailsValidationSummary(Domain.ContestCountingCircleDetails domainCcDetails)
    {
        var ccDetails = await GetDetails(domainCcDetails.ContestId, domainCcDetails.CountingCircleId);
        return new ValidationSummary(await BuildValidationResults(domainCcDetails, ccDetails));
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(
        Domain.ContestCountingCircleDetails domainDetails,
        ContestCountingCircleDetails ccDetails)
    {
        domainDetails.CountOfVotersInformation.TotalCountOfVoters = domainDetails.CountOfVotersInformation.SubTotalInfo
            .Sum(x => x.CountOfVoters.GetValueOrDefault());

        _mapper.Map(domainDetails, ccDetails);

        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(ccDetails.CountingCircleId, ccDetails.ContestId);
        return await BuildValidationResults(ccDetails);
    }

    private async Task<List<ValidationResult>> BuildValidationResults(ContestCountingCircleDetails ccDetails)
        => _validator.Validate(ccDetails, await BuildValidationContext(ccDetails)).ToList();

    private async Task<ContestCountingCircleDetails> GetDetails(Guid contestId, Guid ccId)
    {
        var testingPhaseEnded = await _contestRepo
            .Query()
            .Where(c => c.Id == contestId)
            .Select(c => (bool?)c.TestingPhaseEnded)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(contestId);

        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, ccId, testingPhaseEnded);
        return await _ccDetailsRepository.GetWithRelatedEntities(ccDetailsId)
          ?? throw new EntityNotFoundException(new { contestId, ccId });
    }

    private async Task<ValidationContext> BuildValidationContext(ContestCountingCircleDetails ccDetails)
    {
        var currentContest = await _contestRepo.GetWithValidationContextData(ccDetails.ContestId)
            ?? throw new EntityNotFoundException(ccDetails.ContestId);

        ContestCountingCircleDetails? previousCcDetails = null;

        if (currentContest.PreviousContestId != null)
        {
            var previousCcDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(currentContest.PreviousContestId.Value, ccDetails.CountingCircleId, true);
            previousCcDetails = await _ccDetailsRepository.GetWithRelatedEntities(previousCcDetailsId);
        }

        ccDetails.Contest = currentContest;

        return new ValidationContext(
            currentContest.DomainOfInfluence,
            ccDetails,
            previousCcDetails);
    }
}
