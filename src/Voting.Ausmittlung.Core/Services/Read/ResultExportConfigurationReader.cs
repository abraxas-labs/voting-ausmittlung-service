// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultExportConfigurationReader
{
    private readonly IDbRepository<DataContext, ResultExportConfiguration> _repo;
    private readonly PermissionService _permissionService;

    public ResultExportConfigurationReader(
        IDbRepository<DataContext, ResultExportConfiguration> repo,
        PermissionService permissionService)
    {
        _repo = repo;
        _permissionService = permissionService;
    }

    public Task<List<ResultExportConfiguration>> List(Guid contestId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        return _repo.Query()
            .Include(x => x.PoliticalBusinesses)
            .Where(x => x.ContestId == contestId && x.DomainOfInfluence.SecureConnectId == _permissionService.TenantId)
            .OrderBy(x => x.Description)
            .ToListAsync();
    }
}
