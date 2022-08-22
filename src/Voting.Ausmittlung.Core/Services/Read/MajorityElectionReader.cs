// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
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

public class MajorityElectionReader
{
    private readonly IDbRepository<DataContext, MajorityElection> _electionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _resultRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryElectionRepo;
    private readonly PermissionService _permissionService;

    public MajorityElectionReader(
        IDbRepository<DataContext, MajorityElection> electionRepo,
        IDbRepository<DataContext, MajorityElectionResult> resultRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryElectionRepo,
        PermissionService permissionService)
    {
        _electionRepo = electionRepo;
        _resultRepo = resultRepo;
        _secondaryElectionRepo = secondaryElectionRepo;
        _permissionService = permissionService;
    }

    public async Task<MajorityElection> GetWithCandidates(Guid electionId, bool includeSecondary)
    {
        var query = _electionRepo.Query().AsSplitQuery();
        if (includeSecondary)
        {
            query = query.Include(x => x.SecondaryMajorityElections)
                .ThenInclude(x => x.Candidates.OrderBy(y => y.Position))
                .ThenInclude(x => x.Translations);
        }

        var election = await query
                           .Include(x => x.MajorityElectionCandidates.OrderBy(y => y.Position)).ThenInclude(x => x.Translations)
                           .FirstOrDefaultAsync(x => x.Id == electionId)
                       ?? throw new EntityNotFoundException(nameof(MajorityElection), electionId);

        await EnsureCanReadElection(election.Id, election.ContestId);
        return election;
    }

    public async Task<SecondaryMajorityElection> GetSecondaryWithCandidates(Guid secondaryElectionId)
    {
        var election = await _secondaryElectionRepo.Query()
                           .AsSplitQuery()
                           .Include(x => x.Candidates.OrderBy(y => y.Position))
                           .ThenInclude(x => x.Translations)
                           .Include(x => x.PrimaryMajorityElection)
                           .FirstOrDefaultAsync(x => x.Id == secondaryElectionId)
                       ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElection), secondaryElectionId);
        await EnsureCanReadElection(election.PrimaryMajorityElectionId, election.ContestId);
        return election;
    }

    private async Task EnsureCanReadElection(Guid electionId, Guid contestId)
    {
        _permissionService.EnsureAnyRole();

        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);
        var hasAccess = await _resultRepo.Query()
            .AnyAsync(x => x.MajorityElectionId == electionId && countingCircleIds.Contains(x.CountingCircleId));
        if (!hasAccess)
        {
            throw new ForbiddenException($"no access to election with id {electionId}");
        }
    }
}
