﻿// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using AutoMapper;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using ValidationContext = Voting.Ausmittlung.Core.Services.Validation.Models.ValidationContext;

namespace Voting.Ausmittlung.Core.Services.Validation;

public abstract class CountingCircleResultValidationResultsBuilder<T>
    where T : CountingCircleResult
{
    private readonly ContestRepo _contestRepo;
    private readonly ContestCountingCircleDetailsRepo _contestCountingCircleDetailsRepo;
    private readonly PermissionService _permissionService;

    protected CountingCircleResultValidationResultsBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        IMapper mapper,
        PermissionService permissionService)
    {
        Mapper = mapper;
        _contestRepo = contestRepo;
        _contestCountingCircleDetailsRepo = contestCountingCircleDetailsRepo;
        _permissionService = permissionService;
    }

    protected IMapper Mapper { get; }

    protected void EnsureValidationPermissions(T result)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        _permissionService.EnsureHasPermissionsOnCountingCircle(result.CountingCircle);
    }

    protected async Task<ValidationContext> BuildValidationContext(
        Guid contestId,
        Guid basisCcId,
        PoliticalBusinessType politicalBusinessType,
        bool isDetailedEntry,
        DomainOfInfluenceType politicalBusinessDomainOfInfluenceType)
    {
        var contest = await _contestRepo.GetWithValidationContextData(contestId)
            ?? throw new EntityNotFoundException(contestId);
        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, basisCcId, contest.TestingPhaseEnded);
        var ccDetails = await _contestCountingCircleDetailsRepo.GetWithResults(ccDetailsId)
            ?? new ContestCountingCircleDetails();
        ccDetails.Contest = contest;

        return new ValidationContext(
            contest.DomainOfInfluence,
            ccDetails)
        {
            PoliticalBusinessType = politicalBusinessType,
            IsDetailedEntry = isDetailedEntry,
            PoliticalBusinessDomainOfInfluenceType = politicalBusinessDomainOfInfluenceType,
        };
    }
}
