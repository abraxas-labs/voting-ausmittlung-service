// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class SimpleCountingCircleResultReader
{
    private readonly SimpleCountingCircleResultRepo _repo;
    private readonly PermissionService _permissionService;

    public SimpleCountingCircleResultReader(
        SimpleCountingCircleResultRepo repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public async Task<ContestCantonDefaults> GetCantonDefaults(Guid resultId)
    {
        var simpleCountingCircleResult = await _repo.Query()
            .AsSplitQuery()
            .Include(x => x.PoliticalBusiness!.Contest.CantonDefaults)
            .FirstOrDefaultAsync(x => x.Id == resultId)
                ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), resultId);

        await _permissionService.EnsureCanReadCountingCircle(simpleCountingCircleResult.CountingCircleId, simpleCountingCircleResult.PoliticalBusiness!.ContestId);
        return simpleCountingCircleResult.PoliticalBusiness.Contest.CantonDefaults;
    }
}
