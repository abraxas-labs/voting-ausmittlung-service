// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    private readonly IDbRepository<DataContext, SecondaryMajorityElection> _secondaryMajorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionWriteInMapping> _majorityWriteInMappingRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> _majoritySecondaryWriteInMappingRepo;

    public ResultImportReader(
        PermissionService permissionService,
        IDbRepository<DataContext, ResultImport> resultImportRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, SecondaryMajorityElection> secondaryMajorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionWriteInMapping> majorityWriteInMappingRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> majoritySecondaryWriteInMappingRepo)
    {
        _permissionService = permissionService;
        _resultImportRepo = resultImportRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _secondaryMajorityElectionRepo = secondaryMajorityElectionRepo;
        _majorityWriteInMappingRepo = majorityWriteInMappingRepo;
        _majoritySecondaryWriteInMappingRepo = majoritySecondaryWriteInMappingRepo;
    }

    public async Task<List<ResultImport>> ListEVotingImports(Guid contestId)
    {
        await _permissionService.EnsureIsContestManager(contestId);
        return await GetResultImports(ResultImportType.EVoting, contestId, null);
    }

    public async Task<List<ResultImport>> ListECountingImports(Guid contestId, Guid countingCircleId)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(countingCircleId, contestId);
        return await GetResultImports(ResultImportType.ECounting, contestId, countingCircleId);
    }

    public async Task<List<MajorityElectionGroupedWriteInMappings>> GetMajorityElectionWriteInMappings(
        Guid contestId,
        Guid countingCircleBasisId,
        Guid? electionId,
        ResultImportType? importType)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(countingCircleBasisId, contestId);

        Dictionary<Guid, MajorityElectionBase> politicalBusinessesById;
        if (electionId.HasValue)
        {
            MajorityElectionBase? election = await _majorityElectionRepo.Query()
                .AsSplitQuery()
                .Include(x => x.Translations)
                .Include(x => x.DomainOfInfluence)
                .Include(x => x.Contest.CantonDefaults)
                .FirstOrDefaultAsync(x =>
                    x.Id == electionId
                    && x.ContestId == contestId
                    && x.Active
                    && x.Results.Any(cc => cc.CountingCircle.BasisCountingCircleId == countingCircleBasisId));
            election ??= await _secondaryMajorityElectionRepo.Query()
                .AsSplitQuery()
                .Include(x => x.Translations)
                .Include(x => x.PrimaryMajorityElection.DomainOfInfluence)
                .Include(x => x.PrimaryMajorityElection.Contest.CantonDefaults)
                .FirstOrDefaultAsync(x =>
                    x.Id == electionId
                    && x.PrimaryMajorityElection.ContestId == contestId
                    && x.Active
                    && x.PrimaryMajorityElection.Results.Any(cc => cc.CountingCircle.BasisCountingCircleId == countingCircleBasisId));
            if (election == null)
            {
                return [];
            }

            politicalBusinessesById = new Dictionary<Guid, MajorityElectionBase> { { election.Id, election } };
        }
        else
        {
            var elections = await _majorityElectionRepo.Query()
                .AsSplitQuery()
                .Include(x => x.Translations)
                .Include(x => x.SecondaryMajorityElections.Where(y => y.Active))
                .ThenInclude(x => x.Translations)
                .Include(x => x.DomainOfInfluence)
                .Include(x => x.Contest.CantonDefaults)
                .Where(x => x.ContestId == contestId &&
                            x.Active &&
                            x.Results.Any(cc => cc.CountingCircle.BasisCountingCircleId == countingCircleBasisId))
                .ToListAsync();

            politicalBusinessesById = elections
                .SelectMany(e => e.SecondaryMajorityElections.Cast<MajorityElectionBase>().Prepend(e))
                .ToDictionary(x => x.Id);
        }

        var writeInMappingsQuery = _majorityWriteInMappingRepo.Query()
            .Include(x => x.Result)
            .Include(x => x.CandidateResult)
            .Where(x => politicalBusinessesById.Keys.Contains(x.Result.MajorityElectionId) && x.Result.CountingCircle.BasisCountingCircleId == countingCircleBasisId);

        if (importType.HasValue)
        {
            writeInMappingsQuery = writeInMappingsQuery.Where(x => x.ImportType == importType.Value);
        }

        var writeInMappings = await writeInMappingsQuery
            .OrderBy(x => x.Result.MajorityElection.DomainOfInfluence.Type)
            .ThenBy(x => x.Result.MajorityElection.PoliticalBusinessNumber)
            .ThenBy(x => x.ImportType)
            .ThenByDescending(x => x.VoteCount)
            .ThenBy(x => x.WriteInCandidateName)
            .ToListAsync();

        var secondaryWriteInMappingsQuery = _majoritySecondaryWriteInMappingRepo.Query()
            .Include(x => x.Result)
            .Include(x => x.CandidateResult)
            .Where(x => politicalBusinessesById.Keys.Contains(x.Result.SecondaryMajorityElectionId) && x.Result.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleBasisId);

        if (importType.HasValue)
        {
            secondaryWriteInMappingsQuery = secondaryWriteInMappingsQuery.Where(x => x.ImportType == importType.Value);
        }

        var secondaryWriteInMappings = await secondaryWriteInMappingsQuery
            .OrderBy(x => x.Result.SecondaryMajorityElection.PrimaryMajorityElection.DomainOfInfluence.Type)
            .ThenBy(x => x.Result.SecondaryMajorityElection.PoliticalBusinessNumber)
            .ThenBy(x => x.ImportType)
            .ThenByDescending(x => x.VoteCount)
            .ThenBy(x => x.WriteInCandidateName)
            .ToListAsync();

        return writeInMappings.Cast<MajorityElectionWriteInMappingBase>()
            .Concat(secondaryWriteInMappings)
            .Where(x => !electionId.HasValue || x.PoliticalBusinessId == electionId)
            .GroupBy(x => (x.ImportType, x.ImportId, x.PoliticalBusinessId))
            .Select(writeInGroup => new MajorityElectionGroupedWriteInMappings(
                writeInGroup.Key.ImportId,
                writeInGroup.Key.ImportType,
                politicalBusinessesById[writeInGroup.Key.PoliticalBusinessId],
                writeInGroup.ToList()))
            .ToList();
    }

    private async Task<List<ResultImport>> GetResultImports(
        ResultImportType importType,
        Guid contestId,
        Guid? basisCountingCircleId)
    {
        var query = _resultImportRepo.Query()
            .AsSplitQuery()
            .Include(x => x.IgnoredCountingCircles.OrderBy(cc => cc.CountingCircleId))
            .Include(x => x.ImportedCountingCircles.OrderBy(cc => cc.CountingCircle!.Name))
            .ThenInclude(x => x.CountingCircle)
            .Where(x => x.ImportType == importType && x.ContestId == contestId);

        if (basisCountingCircleId.HasValue)
        {
            query = query.Where(x => x.CountingCircle!.BasisCountingCircleId == basisCountingCircleId.Value);
        }

        return await query
            .OrderByDescending(x => x.Started)
            .ToListAsync();
    }
}
