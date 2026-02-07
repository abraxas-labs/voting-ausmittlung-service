// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
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

public class EVotingResultImportWriter
{
    private readonly AppConfig _appConfig;
    private readonly ILogger<EVotingResultImportWriter> _logger;
    private readonly ResultImportWriter _resultImportWriter;
    private readonly PermissionService _permissionService;
    private readonly ContestService _contestService;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly ProportionalElectionResultImportWriter _proportionalElectionResultImportWriter;
    private readonly MajorityElectionResultImportWriter _majorityElectionResultImportWriter;
    private readonly VoteResultImportWriter _voteResultImportWriter;

    public EVotingResultImportWriter(
        AppConfig appConfig,
        ILogger<EVotingResultImportWriter> logger,
        ResultImportWriter resultImportWriter,
        PermissionService permissionService,
        ContestService contestService,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, Contest> contestRepo,
        ProportionalElectionResultImportWriter proportionalElectionResultImportWriter,
        MajorityElectionResultImportWriter majorityElectionResultImportWriter,
        VoteResultImportWriter voteResultImportWriter)
    {
        _appConfig = appConfig;
        _logger = logger;
        _resultImportWriter = resultImportWriter;
        _permissionService = permissionService;
        _contestService = contestService;
        _aggregateRepository = aggregateRepository;
        _contestRepo = contestRepo;
        _proportionalElectionResultImportWriter = proportionalElectionResultImportWriter;
        _majorityElectionResultImportWriter = majorityElectionResultImportWriter;
        _voteResultImportWriter = voteResultImportWriter;
    }

    public async Task Delete(Guid contestId)
    {
        var contest = await _contestRepo
            .Query()
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.CountingCircleDetails)
            .ThenInclude(x => x.CountingCircle)
            .FirstOrDefaultAsync(x => x.Id == contestId)
            ?? throw new EntityNotFoundException(nameof(Contest), contestId);

        _permissionService.EnsureIsContestManager(contest);
        _contestService.EnsureNotLocked(contest);
        if (!contest.EVoting)
        {
            throw new EVotingNotActiveException(nameof(Contest), contestId);
        }

        var countingCircleResultAggregatesToSave = await SetAllResultsToInSubmissionOrCorrection(contest);

        var (_, importsAggregate) = await GetContestImportsAggregate(contest.Id, contest.TestingPhaseEnded, true);
        await _resultImportWriter.CreateDeleteImportAndSave(importsAggregate);

        foreach (var countingCircleResultAggregate in countingCircleResultAggregatesToSave)
        {
            await _aggregateRepository.Save(countingCircleResultAggregate);
        }

        _logger.LogInformation("Deleted e-voting imported data for {ContestId}", contestId);
    }

    public Task Import(ResultImportMeta importMeta)
        => Import(_resultImportWriter.Deserialize(importMeta, true), importMeta);

    internal async Task Import(VotingImport importData, ResultImportMeta importMeta)
    {
        if (importMeta.BasisCountingCircleId.HasValue || importMeta.ImportType != ResultImportType.EVoting)
        {
            throw new ValidationException("Invalid eVoting import.");
        }

        var contest = await _resultImportWriter.LoadContestForImport(importMeta);
        _permissionService.EnsureIsContestManager(contest);
        if (!contest.EVoting)
        {
            throw new EVotingNotActiveException(nameof(Contest), importMeta.ContestId);
        }

        var (emptyCountingCircles, ignoredCountingCircles) = await ValidateAndFilterCountingCircles(contest, importData);
        var resultAggregatesToSave = await SetAllResultsToInSubmissionOrCorrection(contest);
        var importAggregate = await _resultImportWriter.Import(importData, importMeta, contest, emptyCountingCircles, ignoredCountingCircles);
        await SetSuccessorAndSave(importAggregate, contest.TestingPhaseEnded, false);
        await Task.WhenAll(resultAggregatesToSave.Select(agg => _aggregateRepository.Save(agg)));
        await _aggregateRepository.Save(importAggregate);
    }

    private async Task<(List<Guid> Empty, List<IgnoredImportCountingCircle> Ignored)> ValidateAndFilterCountingCircles(
        Contest contest,
        VotingImport data)
    {
        if (data.VotingCards == null)
        {
            throw new ValidationException("Voting cards are required for eVoting imports");
        }

        var testCountingCirclesById = _appConfig.Publisher.TestCountingCircles
            .GetValueOrDefault(contest.DomainOfInfluence.Canton, [])
            .ToDictionary(x => x.Id);

        var relevantCountingCirclesInContest = await _contestRepo.Query()
            .Where(x => x.Id == contest.Id)
            .SelectMany(x => x.SimplePoliticalBusinesses.Where(pb => pb.Active))
            .SelectMany(x => x.SimpleResults)
            .Select(x => x.CountingCircle)
            .Select(x => new
            {
                x!.EVoting,
                x.BasisCountingCircleId,
                x.Id,
                Results = x.SimpleResults.Select(r => new
                {
                    Type = r.PoliticalBusiness!.PoliticalBusinessType,
                    PbId = r.PoliticalBusinessId,
                }),
            })
            .ToListAsync();

        var ccsByBasisId = relevantCountingCirclesInContest
            .DistinctBy(x => x.BasisCountingCircleId)
            .ToDictionary(x => x.BasisCountingCircleId);

        var ignoredCountingCircles = new List<IgnoredImportCountingCircle>();
        var emptyCountingCircles = new List<Guid>();
        var resultKeysToRemove = new List<(Guid BusinessId, string BasisCountingCircleId)>();
        var resultsToAdd = new List<VotingImportPoliticalBusinessResult>();
        var importedVotingCardCcIds = data.VotingCards.Select(x => x.BasisCountingCircleId).ToHashSet();
        foreach (var ccResult in data.PoliticalBusinessResults)
        {
            if (testCountingCirclesById.TryGetValue(ccResult.BasisCountingCircleId, out var testCountingCircle))
            {
                // This is a test counting circle (Testurne), which we have to ignore
                ignoredCountingCircles.Add(new IgnoredImportCountingCircle
                {
                    CountingCircleId = testCountingCircle.Id,
                    CountingCircleDescription = testCountingCircle.Description,
                    IsTestCountingCircle = true,
                });
                resultKeysToRemove.Add((ccResult.PoliticalBusinessId, testCountingCircle.Id));
                continue;
            }

            if (!Guid.TryParse(ccResult.BasisCountingCircleId, out var basisCountingCircleId)
                || !ccsByBasisId.TryGetValue(basisCountingCircleId, out var cc))
            {
                _logger.LogWarning(
                    "Unknown counting circle ID {CountingCircleId} in result import provided, will be ignored",
                    ccResult.BasisCountingCircleId);
                ignoredCountingCircles.Add(new IgnoredImportCountingCircle
                {
                    CountingCircleId = ccResult.BasisCountingCircleId,
                });
                resultKeysToRemove.Add((ccResult.PoliticalBusinessId, ccResult.BasisCountingCircleId));
                continue;
            }

            if (!cc.EVoting)
            {
                throw new EVotingNotActiveException(nameof(CountingCircle), basisCountingCircleId);
            }

            if (!importedVotingCardCcIds.Contains(ccResult.BasisCountingCircleId))
            {
                throw new ValidationException($"Import does not contain voting cards for counting circle {ccResult.BasisCountingCircleId}");
            }

            if (ccResult is VotingImportEmptyResult)
            {
                _logger.LogWarning("Result of counting circle ID {CountingCircleId} is empty", ccResult.BasisCountingCircleId);
                emptyCountingCircles.Add(cc.Id);

                // Remove the empty entry
                resultKeysToRemove.Add((ccResult.PoliticalBusinessId, ccResult.BasisCountingCircleId));

                // Add all other results, since this data is not available in the import
                resultsToAdd.AddRange(cc.Results.Select(
                    r => r.Type == PoliticalBusinessType.Vote
                    ? (VotingImportPoliticalBusinessResult)new VotingImportVoteResult(r.PbId, ccResult.BasisCountingCircleId, [])
                    : new VotingImportElectionResult(r.PbId, ccResult.BasisCountingCircleId, [])));
            }
        }

        data.RemoveResults(resultKeysToRemove);
        data.AddResults(resultsToAdd);
        data.VotingCards.RemoveAll(x => ignoredCountingCircles.Any(ignored => ignored.CountingCircleId == x.BasisCountingCircleId));

        if (data.VotingCards.Count != data.VotingCards.DistinctBy(x => x.BasisCountingCircleId).Count())
        {
            throw new ValidationException("Duplicate counting circle voting cards provided");
        }

        var importBasisCountingCircleIds = data.PoliticalBusinessResults.Select(x => x.BasisCountingCircleId).ToHashSet();
        var firstNotImportedVotingCardBasisCountingCircleId = data.VotingCards.Find(vc => !importBasisCountingCircleIds.Contains(vc.BasisCountingCircleId))?.BasisCountingCircleId;
        if (firstNotImportedVotingCardBasisCountingCircleId != null)
        {
            throw new ValidationException("Voting cards for imported counting circle not found: " + firstNotImportedVotingCardBasisCountingCircleId);
        }

        if (importBasisCountingCircleIds.Count != data.VotingCards.Count)
        {
            throw new ValidationException("Imported counting circles results do not match with voting cards");
        }

        var eVotingBasisCountingCircleIds = relevantCountingCirclesInContest
            .Where(x => x.EVoting)
            .Select(x => x.BasisCountingCircleId)
            .ToHashSet();

        var firstBasisCountingCircleIdNotImported = eVotingBasisCountingCircleIds.FirstOrDefault(ccId => !importBasisCountingCircleIds.Contains(ccId.ToString()));
        if (firstBasisCountingCircleIdNotImported != Guid.Empty)
        {
            throw new ValidationException("Missing counting circle in import data with id " + firstBasisCountingCircleIdNotImported);
        }

        if (eVotingBasisCountingCircleIds.Count != importBasisCountingCircleIds.Count)
        {
            throw new ValidationException("Did not provide results for all counting circles");
        }

        return (emptyCountingCircles.Distinct().ToList(), ignoredCountingCircles.DistinctBy(x => x.CountingCircleId).ToList());
    }

    private async Task<List<CountingCircleResultAggregate>> SetAllResultsToInSubmissionOrCorrection(Contest contest)
    {
        var ccIds = contest.CountingCircleDetails
            .Where(x => x.EVoting)
            .Select(x => x.CountingCircleId)
            .ToList();
        return await _majorityElectionResultImportWriter.SetAllToInSubmissionOrCorrection(contest.Id, contest.TestingPhaseEnded, ccIds)
            .Concat(_proportionalElectionResultImportWriter.SetAllToInSubmissionOrCorrection(contest.Id, contest.TestingPhaseEnded, ccIds))
            .Concat(_voteResultImportWriter.SetAllToInSubmissionOrCorrection(contest.Id, contest.TestingPhaseEnded, ccIds))
            .ToListAsync();
    }

    private async Task SetSuccessorAndSave(ResultImportAggregate import, bool testingPhaseEnded, bool isDelete)
    {
        var (id, importsAggregate) = await GetContestImportsAggregate(import.ContestId, testingPhaseEnded, isDelete);
        await _resultImportWriter.SetSuccessorAndSave(id, importsAggregate, import, !isDelete);
    }

    private async Task<(Guid ImportsAggregateId, ContestResultImportsAggregate ImportsAggregate)> GetContestImportsAggregate(Guid contestId, bool testingPhaseEnded, bool isDelete)
    {
        // legacy aggregates used the contestId as id.
        var id = contestId;
        var importsAggregate = await _aggregateRepository.TryGetById<ContestResultImportsAggregate>(id);
        if (importsAggregate != null)
        {
            return (id, importsAggregate);
        }

        id = AusmittlungUuidV5.BuildContestImports(contestId, testingPhaseEnded);
        importsAggregate = isDelete
            ? await _aggregateRepository.GetById<ContestResultImportsAggregate>(id)
            : await _aggregateRepository.GetOrCreateById<ContestResultImportsAggregate>(id);
        return (id, importsAggregate);
    }
}
