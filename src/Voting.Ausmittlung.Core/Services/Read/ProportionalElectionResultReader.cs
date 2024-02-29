// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ProportionalElectionResultReader
{
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _repo;
    private readonly PermissionService _permissionService;

    public ProportionalElectionResultReader(
        IDbRepository<DataContext, ProportionalElectionResult> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<ProportionalElectionResult> Get(Guid electionResultId)
    {
        return await QueryElectionResult(x => x.Id == electionResultId)
               ?? throw new EntityNotFoundException(electionResultId);
    }

    public async Task<ProportionalElectionResult> Get(Guid electionId, Guid basisCountingCircleId)
    {
        return await QueryElectionResult(x => x.ProportionalElectionId == electionId && x.CountingCircle.BasisCountingCircleId == basisCountingCircleId)
               ?? throw new EntityNotFoundException(new { electionId, basisCountingCircleId });
    }

    public async Task<ProportionalElectionResult> GetWithUnmodifiedLists(Guid electionResultId)
    {
        var electionResult = await _repo.Query()
                                 .AsSplitQuery()
                                 .Include(x => x.CountingCircle)
                                 .Include(x => x.ProportionalElection.Translations)
                                 .Include(x => x.ProportionalElection.Contest.Translations)
                                 .Include(x => x.ProportionalElection.DomainOfInfluence)
                                 .Include(x => x.UnmodifiedListResults).ThenInclude(x => x.List.Translations)
                                 .FirstOrDefaultAsync(x => x.Id == electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(electionResult.CountingCircleId, electionResult.ProportionalElection.ContestId);

        electionResult.UnmodifiedListResults = electionResult.UnmodifiedListResults
            .OrderBy(x => x.List.Position)
            .ToList();

        return electionResult;
    }

    public async Task<IEnumerable<ProportionalElectionListResult>> GetListResults(Guid electionResultId)
    {
        var electionResult = await _repo.Query()
                                 .AsSplitQuery()
                                 .Include(x => x.ListResults).ThenInclude(x => x.List.Translations)
                                 .Include(x => x.ListResults).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.Candidate.Translations)
                                 .Include(x => x.ListResults).ThenInclude(x => x.CandidateResults).ThenInclude(x => x.Candidate.ProportionalElectionList.Translations)
                                 .Include(x => x.ProportionalElection.Translations)
                                 .FirstOrDefaultAsync(x => x.Id == electionResultId)
                             ?? throw new EntityNotFoundException(electionResultId);

        await _permissionService.EnsureCanReadCountingCircle(electionResult.CountingCircleId, electionResult.ProportionalElection.ContestId);

        var listResults = electionResult.ListResults.OrderBy(l => l.List.Position).ToList();

        foreach (var listResult in listResults)
        {
            listResult.CandidateResults = listResult.CandidateResults.OrderBy(x => x.Candidate.Position).ToList();
        }

        return listResults;
    }

    private async Task<ProportionalElectionResult?> QueryElectionResult(
        Expression<Func<ProportionalElectionResult, bool>> predicate)
    {
        var result = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.CountingCircle)
            .Include(x => x.ProportionalElection.Translations)
            .Include(x => x.ProportionalElection.Contest.Translations)
            .Include(x => x.ProportionalElection.DomainOfInfluence)
            .Where(predicate)
            .FirstOrDefaultAsync();
        if (result == null)
        {
            return null;
        }

        await _permissionService.EnsureCanReadCountingCircle(result.CountingCircleId, result.ProportionalElection.ContestId);
        return result;
    }
}
