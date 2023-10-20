// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class CountingCircleResultsValidationSummariesBuilder
{
    private readonly ProportionalElectionResultValidationSummaryBuilder _proportionalElectionValidationSummaryBuilder;
    private readonly VoteResultValidationSummaryBuilder _voteResultValidationSummaryBuilder;
    private readonly MajorityElectionResultValidationSummaryBuilder _majorityElectionValidationSummaryBuilder;
    private readonly ContestRepo _contestRepo;
    private readonly VoteResultRepo _voteResultRepo;
    private readonly ProportionalElectionResultRepo _proportionalElectionResultRepo;
    private readonly MajorityElectionResultRepo _majorityElectionResultRepo;
    private readonly PermissionService _permissionService;
    private readonly ContestCountingCircleDetailsRepo _contestCountingCircleDetailsRepo;
    private readonly ContestService _contestService;

    public CountingCircleResultsValidationSummariesBuilder(
        ProportionalElectionResultValidationSummaryBuilder proportionalElectionValidationSummaryBuilder,
        VoteResultValidationSummaryBuilder voteResultValidationSummaryBuilder,
        MajorityElectionResultValidationSummaryBuilder majorityElectionValidationSummaryBuilder,
        ContestRepo contestRepo,
        VoteResultRepo voteResultRepo,
        ProportionalElectionResultRepo proportionalElectionResultRepo,
        MajorityElectionResultRepo majorityElectionResultRepo,
        PermissionService permissionService,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        ContestService contestService)
    {
        _proportionalElectionValidationSummaryBuilder = proportionalElectionValidationSummaryBuilder;
        _voteResultValidationSummaryBuilder = voteResultValidationSummaryBuilder;
        _majorityElectionValidationSummaryBuilder = majorityElectionValidationSummaryBuilder;
        _contestRepo = contestRepo;
        _voteResultRepo = voteResultRepo;
        _proportionalElectionResultRepo = proportionalElectionResultRepo;
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _permissionService = permissionService;
        _contestCountingCircleDetailsRepo = contestCountingCircleDetailsRepo;
        _contestService = contestService;
    }

    public async Task<List<ValidationSummary>> BuildValidationSummaries(
        Guid contestId,
        Guid basisCcId,
        IReadOnlyCollection<Guid> resultIds)
    {
        var ccDetails = await GetCountingCircleDetails(contestId, basisCcId);
        return await BuildValidationSummaries(ccDetails, resultIds);
    }

    public async Task<List<ValidationSummary>> BuildValidationSummaries(
        ContestCountingCircleDetails ccDetails,
        IReadOnlyCollection<Guid> resultIds)
    {
        EnsureHasPermissions(ccDetails.CountingCircle, ccDetails.Contest);

        var (voteResults, peResults, meResults) = await LoadResults(resultIds, ccDetails.CountingCircleId);
        if (resultIds.Count != (voteResults.Count + peResults.Count + meResults.Count))
        {
            throw new ValidationException("Non existing counting circle result id provided");
        }

        return voteResults.Select(vr => new ValidationSummary(_voteResultValidationSummaryBuilder.BuildValidationResults(vr, ccDetails), vr.PoliticalBusiness.Title))
            .Concat(peResults.Select(per => new ValidationSummary(_proportionalElectionValidationSummaryBuilder.BuildValidationResults(per, ccDetails), per.PoliticalBusiness.Title)))
            .Concat(meResults.Select(mer => new ValidationSummary(_majorityElectionValidationSummaryBuilder.BuildValidationResults(mer, ccDetails), mer.PoliticalBusiness.Title)))
            .OrderBy(x => x.Title)
            .ToList();
    }

    private void EnsureHasPermissions(CountingCircle countingCircle, Contest contest)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(countingCircle, contest);
        _contestService.EnsureNotLocked(contest);
    }

    private async Task<(List<VoteResult> VoteResults, List<ProportionalElectionResult> ProportionalElectionResults, List<MajorityElectionResult> MajorityElectionResults)> LoadResults(
        IReadOnlyCollection<Guid> resultIds,
        Guid ccId)
    {
        var voteResults = await _voteResultRepo
            .ListWithValidationContextData(vr => resultIds.Contains(vr.Id) && vr.CountingCircleId == ccId, false);

        var proportionalElectionResults = await _proportionalElectionResultRepo
            .ListWithValidationContextData(per => resultIds.Contains(per.Id) && per.CountingCircleId == ccId, false);

        var majorityElectionResults = await _majorityElectionResultRepo
            .ListWithValidationContextData(mer => resultIds.Contains(mer.Id) && mer.CountingCircleId == ccId, false);

        return (voteResults, proportionalElectionResults, majorityElectionResults);
    }

    private async Task<ContestCountingCircleDetails> GetCountingCircleDetails(Guid contestId, Guid basisCcId)
    {
        var contest = await _contestRepo.GetWithValidationContextData(contestId)
            ?? throw new EntityNotFoundException(contestId);
        var ccDetailsId = AusmittlungUuidV5.BuildContestCountingCircleDetails(contestId, basisCcId, contest.TestingPhaseEnded);
        var ccDetails = await _contestCountingCircleDetailsRepo.GetWithResults(ccDetailsId)
            ?? throw new EntityNotFoundException(ccDetailsId);
        ccDetails.Contest = contest;
        return ccDetails;
    }
}
