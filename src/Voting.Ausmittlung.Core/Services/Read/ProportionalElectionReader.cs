// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Iam.Exceptions;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ProportionalElectionReader
{
    private readonly IDbRepository<DataContext, ProportionalElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionList> _listRepo;
    private readonly IDbRepository<DataContext, ProportionalElection> _electionRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidate> _candidatesRepo;
    private readonly PermissionService _permissionService;

    public ProportionalElectionReader(
        IDbRepository<DataContext, ProportionalElectionResult> resultRepo,
        IDbRepository<DataContext, ProportionalElectionList> listRepo,
        IDbRepository<DataContext, ProportionalElection> electionRepo,
        IDbRepository<DataContext, ProportionalElectionCandidate> candidatesRepo,
        PermissionService permissionService)
    {
        _resultRepo = resultRepo;
        _listRepo = listRepo;
        _electionRepo = electionRepo;
        _candidatesRepo = candidatesRepo;
        _permissionService = permissionService;
    }

    public async Task<List<ProportionalElectionList>> GetLists(Guid electionId)
    {
        await EnsureCanReadElection(electionId);
        return await _listRepo.Query()
            .Include(l => l.Translations)
            .Where(l => l.ProportionalElectionId == electionId)
            .OrderBy(l => l.Position)
            .ToListAsync();
    }

    public async Task<ProportionalElectionList> GetList(Guid listId)
    {
        var list = await _listRepo.Query()
            .Include(l => l.Translations)
            .FirstOrDefaultAsync(x => x.Id == listId)
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionList), listId);

        await EnsureCanReadElection(list.ProportionalElectionId);
        return list;
    }

    public async Task<List<ProportionalElectionCandidate>> ListCandidates(Guid electionId)
    {
        await EnsureCanReadElection(electionId);
        return await _candidatesRepo.Query()
            .AsSplitQuery()
            .Include(c => c.Translations)
            .Include(c => c.ProportionalElectionList.Translations)
            .Where(c => c.ProportionalElectionList.ProportionalElectionId == electionId)
            .OrderBy(c => c.ProportionalElectionList.Position)
            .ThenBy(c => c.Position)
            .ToListAsync();
    }

    private async Task EnsureCanReadElection(Guid electionId)
    {
        var contestId = await _electionRepo.Query()
            .Where(e => e.Id == electionId)
            .Select(e => e.ContestId)
            .FirstOrDefaultAsync();

        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);
        var hasAccess = await _resultRepo.Query()
            .AnyAsync(x => x.ProportionalElectionId == electionId && countingCircleIds.Contains(x.CountingCircleId));
        if (!hasAccess)
        {
            throw new ForbiddenException($"no access to election with id {electionId}");
        }
    }
}
