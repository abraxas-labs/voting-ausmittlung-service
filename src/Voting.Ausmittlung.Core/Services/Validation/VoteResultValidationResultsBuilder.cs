// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Core.Services.Validation.Models;
using Voting.Ausmittlung.Core.Services.Validation.Validators;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Validation;

public class VoteResultValidationResultsBuilder : CountingCircleResultValidationResultsBuilder<VoteResult>
{
    private readonly IDbRepository<DataContext, VoteResult> _voteResultRepository;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly IValidator<VoteResult> _voteResultValidator;

    public VoteResultValidationResultsBuilder(
        ContestRepo contestRepo,
        ContestCountingCircleDetailsRepo contestCountingCircleDetailsRepo,
        IDbRepository<DataContext, VoteResult> voteResultRepository,
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

    public async Task<List<ValidationResult>> BuildEnterResultsValidationResults(Guid voteResultId, IReadOnlyCollection<VoteBallotResults> ballotResults)
    {
        var countOfVotersEntered = Mapper.Map<IEnumerable<VoteBallotResultsCountOfVotersEventData>>(ballotResults);
        var ballotResultsEntered = Mapper.Map<IEnumerable<VoteBallotResultsEventData>>(ballotResults);
        var voteResult = await GetVoteResult(voteResultId);

        _voteResultBuilder.UpdateResults(voteResult, ballotResultsEntered);
        _voteResultBuilder.UpdateCountOfVoters(voteResult, countOfVotersEntered);

        return await BuildValidationResults(voteResult);
    }

    public async Task<List<ValidationResult>> BuildEnterCountOfVotersValidationResults(Guid voteResultId, IReadOnlyCollection<VoteBallotResultsCountOfVoters> countOfVoters)
    {
        var voteResult = await GetVoteResult(voteResultId);
        var countOfVotersEntered = Mapper.Map<IEnumerable<VoteBallotResultsCountOfVotersEventData>>(countOfVoters);
        _voteResultBuilder.UpdateCountOfVoters(voteResult, countOfVotersEntered);

        return await BuildValidationResults(voteResult);
    }

    internal async Task<List<ValidationResult>> BuildValidationResults(VoteResult voteResult)
    {
        var context = await BuildValidationContext(
            voteResult.Vote.ContestId,
            voteResult.CountingCircle.BasisCountingCircleId,
            PoliticalBusinessType.Vote,
            voteResult.Entry == VoteResultEntry.Detailed,
            voteResult.Vote.DomainOfInfluence.Type);

        return _voteResultValidator.Validate(voteResult, context).ToList();
    }

    private async Task<VoteResult> GetVoteResult(Guid voteResultId)
    {
        var voteResult = await _voteResultRepository.Query()
            .AsSplitQuery()
            .Include(x => x.Vote.DomainOfInfluence)
            .Include(x => x.CountingCircle.ResponsibleAuthority)
            .Include(x => x.Vote.Contest.DomainOfInfluence)
            .Include(x => x.Results).ThenInclude(x => x.Ballot)
            .Include(x => x.Results).ThenInclude(x => x.QuestionResults).ThenInclude(x => x.Question)
            .Include(x => x.Results).ThenInclude(x => x.TieBreakQuestionResults).ThenInclude(x => x.Question)
            .FirstOrDefaultAsync(x => x.Id == voteResultId)
            ?? throw new EntityNotFoundException(voteResultId);

        EnsureValidationPermissions(voteResult);
        return voteResult;
    }
}
