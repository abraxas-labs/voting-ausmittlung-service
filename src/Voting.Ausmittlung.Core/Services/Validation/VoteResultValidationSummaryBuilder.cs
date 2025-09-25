// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class VoteResultValidationSummaryBuilder : CountingCircleResultValidationSummaryBuilder<VoteResult>
{
    private readonly VoteResultRepo _voteResultRepository;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly IValidator<VoteResult> _voteResultValidator;

    public VoteResultValidationSummaryBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        VoteResultRepo voteResultRepository,
        VoteResultBuilder voteResultBuilder,
        IMapper mapper,
        PermissionService permissionService,
        IValidator<VoteResult> voteResultValidator)
        : base(contestRepo, contestCountingCircleDetailsRepo, mapper, permissionService)
    {
        _voteResultRepository = voteResultRepository;
        _voteResultBuilder = voteResultBuilder;
        _voteResultValidator = voteResultValidator;
    }

    public async Task<ValidationSummary> BuildEnterResultsValidationSummary(Guid voteResultId, IReadOnlyCollection<VoteBallotResults> ballotResults)
    {
        var countOfVotersEntered = Mapper.Map<IEnumerable<VoteBallotResultsCountOfVotersEventData>>(ballotResults);
        var ballotResultsEntered = Mapper.Map<IEnumerable<VoteBallotResultsEventData>>(ballotResults);
        var voteResult = await GetVoteResult(voteResultId);

        _voteResultBuilder.UpdateResults(voteResult, ballotResultsEntered);
        _voteResultBuilder.UpdateCountOfVoters(voteResult, countOfVotersEntered);

        return new ValidationSummary(await BuildValidationResults(voteResult));
    }

    public async Task<ValidationSummary> BuildEnterCountOfVotersValidationSummary(Guid voteResultId, IReadOnlyCollection<VoteBallotResultsCountOfVoters> countOfVoters)
    {
        var voteResult = await GetVoteResult(voteResultId);
        var countOfVotersEntered = Mapper.Map<IEnumerable<VoteBallotResultsCountOfVotersEventData>>(countOfVoters);
        _voteResultBuilder.UpdateCountOfVoters(voteResult, countOfVotersEntered);

        return new ValidationSummary(await BuildValidationResults(voteResult));
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(VoteResult voteResult)
    {
        var ccDetails = await GetContestCountingCircleDetails(voteResult.Vote.ContestId, voteResult.CountingCircle.BasisCountingCircleId);
        return BuildValidationResults(voteResult, ccDetails);
    }

    internal List<ValidationResult> BuildValidationResults(VoteResult voteResult, Data.Models.ContestCountingCircleDetails ccDetails)
    {
        var context = BuildValidationContext(
            ccDetails,
            PoliticalBusinessType.Vote,
            voteResult.Entry == VoteResultEntry.Detailed,
            voteResult.Vote.DomainOfInfluence);

        voteResult.CountingCircle = ccDetails.CountingCircle;
        return _voteResultValidator.Validate(voteResult, context).ToList();
    }

    private async Task<VoteResult> GetVoteResult(Guid voteResultId)
    {
        var voteResult = (await _voteResultRepository
            .ListWithValidationContextData(x => x.Id == voteResultId, true))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException(voteResultId);

        EnsureValidationPermissions(voteResult);
        return voteResult;
    }
}
