// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Iam.Exceptions;

namespace Voting.Ausmittlung.Core.Services.Read;

public class SimplePoliticalBusinessReader
{
    private readonly SimplePoliticalBusinessRepo _repo;
    private readonly SimpleCountingCircleResultRepo _simpleCountingCircleResultRepo;
    private readonly PermissionService _permissionService;

    public SimplePoliticalBusinessReader(
        SimplePoliticalBusinessRepo repo,
        SimpleCountingCircleResultRepo simpleCountingCircleResultRepo,
        PermissionService permissionService)
    {
        _repo = repo;
        _simpleCountingCircleResultRepo = simpleCountingCircleResultRepo;
        _permissionService = permissionService;
    }

    public async Task<ContestCantonDefaults> GetCantonDefaults(Guid politicalBusinessId)
    {
        var simplePoliticalBusiness = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.Contest.CantonDefaults)
            .FirstOrDefaultAsync(x => x.Id == politicalBusinessId)
                ?? throw new EntityNotFoundException(nameof(SimplePoliticalBusiness), politicalBusinessId);

        await EnsureCanReadPoliticalBusiness(politicalBusinessId, simplePoliticalBusiness.ContestId);
        return simplePoliticalBusiness.Contest.CantonDefaults;
    }

    private async Task EnsureCanReadPoliticalBusiness(Guid politicalBusinessId, Guid contestId)
    {
        var countingCircleIds = await _permissionService.GetReadableCountingCircleIds(contestId);
        var hasAccess = await _simpleCountingCircleResultRepo.Query()
            .AnyAsync(x => x.PoliticalBusinessId == politicalBusinessId && countingCircleIds.Contains(x.CountingCircleId));
        if (!hasAccess)
        {
            throw new ForbiddenException($"no access to political business with id {politicalBusinessId}");
        }
    }
}
