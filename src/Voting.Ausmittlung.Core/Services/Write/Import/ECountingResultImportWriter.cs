// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Utils;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Persistence;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class ECountingResultImportWriter
{
    private readonly ILogger<EVotingResultImportWriter> _logger;
    private readonly ResultImportWriter _resultImportWriter;
    private readonly PermissionService _permissionService;
    private readonly ContestService _contestService;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly ProportionalElectionResultImportWriter _proportionalElectionResultImportWriter;
    private readonly MajorityElectionResultImportWriter _majorityElectionResultImportWriter;
    private readonly VoteResultImportWriter _voteResultImportWriter;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultsRepo;
    private readonly IDbRepository<DataContext, ResultImport> _resultImportRepo;

    public ECountingResultImportWriter(
        ILogger<EVotingResultImportWriter> logger,
        ResultImportWriter resultImportWriter,
        PermissionService permissionService,
        ContestService contestService,
        IAggregateRepository aggregateRepository,
        ProportionalElectionResultImportWriter proportionalElectionResultImportWriter,
        MajorityElectionResultImportWriter majorityElectionResultImportWriter,
        VoteResultImportWriter voteResultImportWriter,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultsRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, ResultImport> resultImportRepo)
    {
        _logger = logger;
        _resultImportWriter = resultImportWriter;
        _permissionService = permissionService;
        _contestService = contestService;
        _aggregateRepository = aggregateRepository;
        _proportionalElectionResultImportWriter = proportionalElectionResultImportWriter;
        _majorityElectionResultImportWriter = majorityElectionResultImportWriter;
        _voteResultImportWriter = voteResultImportWriter;
        _simpleResultsRepo = simpleResultsRepo;
        _contestRepo = contestRepo;
        _resultImportRepo = resultImportRepo;
    }

    public async Task DeletePoliticalBusinessImportData(
        Guid contestId,
        Guid basisCountingCircleId,
        Guid politicalBusinessId)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, contestId);

        var contest = await _contestRepo.GetByKey(contestId)
                      ?? throw new EntityNotFoundException(nameof(Contest), contestId);
        _contestService.EnsureNotLocked(contest);

        var id = AusmittlungUuidV5.BuildContestCountingCircleImports(contestId, basisCountingCircleId, contest.TestingPhaseEnded);
        var countingCircleId = AusmittlungUuidV5.BuildCountingCircleSnapshot(contestId, basisCountingCircleId);
        var resultImports = await _aggregateRepository.GetById<CountingCircleResultImportsAggregate>(id);

        var hasResultImportWithPoliticalBusinessImported = await _resultImportRepo.Query()
            .AsSplitQuery()
            .OrderByDescending(i => i.Started)
            .Include(i => i.ImportedPoliticalBusinesses)
            .AnyAsync(i => i.ContestId == contestId
                && i.CountingCircleId == countingCircleId
                && i.ImportedPoliticalBusinesses.Any(ipb => ipb.PoliticalBusinessId == politicalBusinessId && ipb.PoliticalBusiness!.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
                && !i.Deleted);

        if (!hasResultImportWithPoliticalBusinessImported)
        {
            throw new EntityNotFoundException(nameof(ResultImport), new[] { contestId, politicalBusinessId, countingCircleId });
        }

        await EnsureAllResultsInSubmissionOrCorrection(contestId, basisCountingCircleId, new HashSet<Guid> { politicalBusinessId });
        await _resultImportWriter.CreatePoliticalBusinessDeleteImportAndSave(resultImports, politicalBusinessId);
        _logger.LogInformation("Deleted e-counting imported data for political business {PoliticalBusinessId} in counting circle  {CountingCircleId}", politicalBusinessId, basisCountingCircleId);
    }

    public async Task Import(ResultImportMeta importMeta)
    {
        if (!importMeta.BasisCountingCircleId.HasValue || importMeta.ImportType != ResultImportType.ECounting)
        {
            throw new ValidationException("Invalid eCounting import.");
        }

        var contest = await _resultImportWriter.LoadContestForImport(importMeta);
        var ccDetail = contest.CountingCircleDetails.FirstOrDefault();
        if (ccDetail?.ECounting != true)
        {
            throw new ECountingImportDisabledException(importMeta.BasisCountingCircleId.Value);
        }

        _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircle(ccDetail.CountingCircle, contest);
        var importData = _resultImportWriter.Deserialize(importMeta, false);
        var ignoredPoliticalBusinesses = await FilterPoliticalBusinessResults(importData, ccDetail);
        ValidateCountingCircles(importMeta.BasisCountingCircleId.Value, importData.PoliticalBusinessResults);

        var importAggregate = await _resultImportWriter.Import(importData, importMeta, contest, [], [], ignoredPoliticalBusinesses);

        // secondary elections are part of the primary election aggregate
        // the business type is only set during Import, therefore validate after the import,
        // but before the save.
        var politicalBusinessIds = importData.PoliticalBusinessResults
            .Where(x => x.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
            .Select(x => x.PoliticalBusinessId)
            .ToHashSet();
        await EnsureAllResultsInSubmissionOrCorrection(contest.Id, importMeta.BasisCountingCircleId.Value, politicalBusinessIds);

        var importsId = AusmittlungUuidV5.BuildContestCountingCircleImports(contest.Id, importMeta.BasisCountingCircleId.Value, contest.TestingPhaseEnded);
        var imports = await _aggregateRepository.GetOrCreateById<CountingCircleResultImportsAggregate>(importsId);
        await _resultImportWriter.SetSuccessorAndSave(importsId, imports, importAggregate, true);
        await _aggregateRepository.Save(importAggregate);
    }

    private void ValidateCountingCircles(Guid basisCountingCircleId, IEnumerable<VotingImportPoliticalBusinessResult> results)
    {
        foreach (var result in results)
        {
            if (!Guid.TryParse(result.BasisCountingCircleId, out var resultCountingCircleId)
                || resultCountingCircleId != basisCountingCircleId)
            {
                throw new UnknownCountingCircleInImportException(resultCountingCircleId, basisCountingCircleId);
            }
        }
    }

    private async Task EnsureAllResultsInSubmissionOrCorrection(Guid contestId, Guid basisCountingCircleId, IReadOnlySet<Guid> politicalBusinessIds)
    {
        // secondary majority elections are part of the primary majority election aggregate
        var results = await _simpleResultsRepo
            .Query()
            .Where(x => x.CountingCircle!.SnapshotContestId == contestId
                        && x.CountingCircle!.BasisCountingCircleId == basisCountingCircleId
                        && politicalBusinessIds.Contains(x.PoliticalBusinessId)
                        && x.PoliticalBusiness!.PoliticalBusinessType != PoliticalBusinessType.SecondaryMajorityElection)
            .Select(x => new { x.Id, x.PoliticalBusinessId, x.PoliticalBusiness!.PoliticalBusinessType })
            .ToListAsync();

        var notFoundPoliticalBusinessIds = politicalBusinessIds.ToHashSet();
        foreach (var result in results)
        {
            notFoundPoliticalBusinessIds.Remove(result.PoliticalBusinessId);
        }

        if (notFoundPoliticalBusinessIds.Count > 0)
        {
            throw new ValidationException($"Unknown political business with id {politicalBusinessIds.First()} in import.");
        }

        var resultsByType = results.GroupBy(x => x.PoliticalBusinessType)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Id).ToList());

        var counts = await Task.WhenAll(
            _majorityElectionResultImportWriter.EnsureInSubmissionOrCorrection(resultsByType.GetValueOrDefault(PoliticalBusinessType.MajorityElection) ?? []),
            _proportionalElectionResultImportWriter.EnsureInSubmissionOrCorrection(resultsByType.GetValueOrDefault(PoliticalBusinessType.ProportionalElection) ?? []),
            _voteResultImportWriter.EnsureInSubmissionOrCorrection(resultsByType.GetValueOrDefault(PoliticalBusinessType.Vote) ?? []));
        if (politicalBusinessIds.Count != counts.Sum())
        {
            throw new ValidationException("Unexpected number of results during eCounting import, there may be an unexpected id in the import data or the result is in an unexpected state.");
        }
    }

    private async Task<ISet<Guid>> FilterPoliticalBusinessResults(
        VotingImport importData,
        ContestCountingCircleDetails ccDetail)
    {
        var basisCcId = ccDetail.CountingCircle.BasisCountingCircleId.ToString();

        var alreadyImportedPoliticalBusinesses = await _simpleResultsRepo.Query()
            .Where(r => r.CountingCircleId == ccDetail.CountingCircleId && r.ECountingImported)
            .Select(r => r.PoliticalBusinessId)
            .ToListAsync();

        var ignoredPoliticalBusinessIds = new HashSet<Guid>();

        foreach (var pbResult in importData.PoliticalBusinessResults)
        {
            if (!alreadyImportedPoliticalBusinesses.Contains(pbResult.PoliticalBusinessId))
            {
                continue;
            }

            ignoredPoliticalBusinessIds.Add(pbResult.PoliticalBusinessId);
        }

        importData.RemoveResults(ignoredPoliticalBusinessIds.Select(pbId => (pbId, basisCcId)));
        return ignoredPoliticalBusinessIds;
    }
}
