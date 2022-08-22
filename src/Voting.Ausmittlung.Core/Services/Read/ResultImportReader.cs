// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultImportReader
{
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, ResultImport> _resultImportRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionWriteInMapping> _majorityWriteInMappingRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> _majoritySecondaryWriteInMappingRepo;

    public ResultImportReader(
        PermissionService permissionService,
        IDbRepository<DataContext, ResultImport> resultImportRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionWriteInMapping> majorityWriteInMappingRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> majoritySecondaryWriteInMappingRepo)
    {
        _permissionService = permissionService;
        _resultImportRepo = resultImportRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _majorityWriteInMappingRepo = majorityWriteInMappingRepo;
        _majoritySecondaryWriteInMappingRepo = majoritySecondaryWriteInMappingRepo;
    }

    public async Task<List<ResultImport>> GetResultImports(Guid contestId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();
        await _permissionService.EnsureIsContestManager(contestId);
        return await _resultImportRepo.Query()
            .Where(x => x.ContestId == contestId)
            .OrderByDescending(x => x.Started)
            .ToListAsync();
    }

    public async Task<ImportMajorityElectionWriteInMappings> GetMajorityElectionWriteInMappings(Guid contestId, Guid countingCircleBasisId)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        await _permissionService.EnsureHasPermissionsOnCountingCircleWithBasisId(countingCircleBasisId, contestId);

        var importId = await GetLatestResultImportId(contestId)
                       ?? throw new EntityNotFoundException(nameof(ResultImport), new { contestId });

        var elections = await _majorityElectionRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.SecondaryMajorityElections.Where(y => y.Active).OrderBy(y => y.PoliticalBusinessNumber))
            .ThenInclude(x => x.Translations)
            .Where(x => x.ContestId == contestId &&
                        x.Active &&
                        x.Results.Any(cc => cc.CountingCircle.BasisCountingCircleId == countingCircleBasisId))
            .OrderBy(x => x.DomainOfInfluence.Type)
            .ThenBy(x => x.PoliticalBusinessNumber)
            .ToListAsync();

        var majorityElectionIds = elections
            .Select(x => x.Id)
            .ToHashSet();

        var secondaryMajorityElectionIds = elections
            .SelectMany(x => x.SecondaryMajorityElections)
            .Select(x => x.Id)
            .ToHashSet();

        var writeInMappings = await _majorityWriteInMappingRepo.Query()
            .Include(x => x.Result)
            .Include(x => x.CandidateResult)
            .Where(x => majorityElectionIds.Contains(x.Result.MajorityElectionId) && x.Result.CountingCircle.BasisCountingCircleId == countingCircleBasisId)
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.WriteInCandidateName)
            .ToListAsync();
        var writeInMappingsByMajorityElectionId = writeInMappings
            .GroupBy(x => x.Result.MajorityElectionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var secondaryWriteInMappings = await _majoritySecondaryWriteInMappingRepo.Query()
            .Include(x => x.Result)
            .Include(x => x.CandidateResult)
            .Where(x => secondaryMajorityElectionIds.Contains(x.Result.SecondaryMajorityElectionId) && x.Result.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleBasisId)
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.WriteInCandidateName)
            .ToListAsync();
        var secondaryWriteInMappingsByMajorityElectionId = secondaryWriteInMappings
            .GroupBy(x => x.Result.SecondaryMajorityElectionId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var politicalBusinesses = elections.SelectMany(e => e.SecondaryMajorityElections.Cast<MajorityElectionBase>().Prepend(e));
        var mappingGroups = new List<MajorityElectionGroupedWriteInMappings>();
        foreach (var politicalBusiness in politicalBusinesses)
        {
            IReadOnlyCollection<MajorityElectionWriteInMappingBase>? writeIns = writeInMappingsByMajorityElectionId.GetValueOrDefault(politicalBusiness.Id);
            writeIns ??= secondaryWriteInMappingsByMajorityElectionId.GetValueOrDefault(politicalBusiness.Id);
            if (writeIns == null)
            {
                continue;
            }

            mappingGroups.Add(new MajorityElectionGroupedWriteInMappings(
                politicalBusiness,
                writeIns));
        }

        return new ImportMajorityElectionWriteInMappings(importId, mappingGroups);
    }

    private async Task<Guid?> GetLatestResultImportId(Guid contestId)
    {
        return await _resultImportRepo.Query()
            .Where(x => x.ContestId == contestId)
            .OrderByDescending(x => x.Started)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
    }
}
