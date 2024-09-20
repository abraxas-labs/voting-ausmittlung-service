// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.Services.Read;

public class ResultImportReader
{
    private readonly PermissionService _permissionService;
    private readonly IDbRepository<DataContext, ResultImport> _resultImportRepo;
    private readonly IDbRepository<DataContext, MajorityElection> _majorityElectionRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _majorityElectionResultRepo;
    private readonly IDbRepository<DataContext, MajorityElectionWriteInMapping> _majorityWriteInMappingRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> _majoritySecondaryWriteInMappingRepo;
    private readonly ILogger<ResultImportReader> _logger;
    private readonly MessageConsumerHub<ResultImportChanged> _resultImportChangeConsumer;
    private readonly MessageConsumerHub<WriteInMappingsChanged> _writeInMappingChangeConsumer;

    public ResultImportReader(
        PermissionService permissionService,
        IDbRepository<DataContext, ResultImport> resultImportRepo,
        IDbRepository<DataContext, MajorityElection> majorityElectionRepo,
        IDbRepository<DataContext, MajorityElectionResult> majorityElectionResultRepo,
        IDbRepository<DataContext, MajorityElectionWriteInMapping> majorityWriteInMappingRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> majoritySecondaryWriteInMappingRepo,
        ILogger<ResultImportReader> logger,
        MessageConsumerHub<ResultImportChanged> resultImportChangeConsumer,
        MessageConsumerHub<WriteInMappingsChanged> writeInMappingChangeConsumer)
    {
        _permissionService = permissionService;
        _resultImportRepo = resultImportRepo;
        _majorityElectionRepo = majorityElectionRepo;
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _majorityWriteInMappingRepo = majorityWriteInMappingRepo;
        _majoritySecondaryWriteInMappingRepo = majoritySecondaryWriteInMappingRepo;
        _logger = logger;
        _resultImportChangeConsumer = resultImportChangeConsumer;
        _writeInMappingChangeConsumer = writeInMappingChangeConsumer;
    }

    public async Task<List<ResultImport>> GetResultImports(Guid contestId)
    {
        await _permissionService.EnsureIsContestManager(contestId);
        return await _resultImportRepo.Query()
            .AsSplitQuery()
            .Include(x => x.IgnoredCountingCircles.OrderBy(cc => cc.CountingCircleId))
            .Include(x => x.ImportedCountingCircles.OrderBy(cc => cc.CountingCircle!.Name))
            .ThenInclude(x => x.CountingCircle)
            .Where(x => x.ContestId == contestId)
            .OrderByDescending(x => x.Started)
            .ToListAsync();
    }

    public async Task<ImportMajorityElectionWriteInMappings> GetMajorityElectionWriteInMappings(Guid contestId, Guid countingCircleBasisId)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(countingCircleBasisId, contestId);

        var importId = await GetLatestResultImportId(contestId)
                       ?? throw new EntityNotFoundException(nameof(ResultImport), new { contestId });

        var elections = await _majorityElectionRepo.Query()
            .AsSplitQuery()
            .Include(x => x.Translations)
            .Include(x => x.SecondaryMajorityElections.Where(y => y.Active).OrderBy(y => y.PoliticalBusinessNumber))
            .ThenInclude(x => x.Translations)
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.Contest.CantonDefaults)
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

    public async Task ListenToWriteInMappingChanges(
        Guid contestId,
        Guid countingCircleId,
        Func<WriteInMappingsChanged, Task> listener,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listening to write in mapping changes for counting circle with id {CountingCircleId}", countingCircleId);
        await _permissionService.EnsureCanReadBasisCountingCircle(countingCircleId, contestId);
        _logger.LogDebug("Listening permission is assured.");

        var resultIds = await _majorityElectionResultRepo.Query()
            .Where(x => x.MajorityElection.ContestId == contestId && x.CountingCircle.BasisCountingCircleId == countingCircleId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var resultIdSet = new HashSet<Guid>(resultIds);

        await _writeInMappingChangeConsumer.Listen(
            b => resultIdSet.Contains(b.ElectionResultId),
            listener,
            cancellationToken);
    }

    public async Task ListenToResultImportChanges(
        Guid contestId,
        Guid countingCircleId,
        Func<ResultImportChanged, Task> listener,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listening to result import changes for counting circle with id {CountingCircleId}", countingCircleId);

        await _permissionService.EnsureCanReadBasisCountingCircle(countingCircleId, contestId);

        _logger.LogDebug("Listening permission is assured.");

        await _resultImportChangeConsumer.Listen(
            e => contestId == e.ContestId && countingCircleId == e.CountingCircleId,
            listener,
            cancellationToken);
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
