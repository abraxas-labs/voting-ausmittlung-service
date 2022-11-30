// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using eCH_0222_1_0.Standard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Domain.Aggregate;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Models.Import;
using Voting.Ausmittlung.Core.Services.Permission;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Ech.Converters;
using Voting.Ausmittlung.Ech.Models;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Eventing.Persistence;
using ContestCountingCircleDetails = Voting.Ausmittlung.Data.Models.ContestCountingCircleDetails;

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class ResultImportWriter
{
    private readonly ILogger<ResultImportWriter> _logger;
    private readonly ContestService _contestService;
    private readonly PermissionService _permissionService;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly ProportionalElectionResultImportWriter _proportionalElectionResultImportWriter;
    private readonly MajorityElectionResultImportWriter _majorityElectionResultImportWriter;
    private readonly SecondaryMajorityElectionResultImportWriter _secondaryMajorityElectionResultImportWriter;
    private readonly VoteResultImportWriter _voteResultImportWriter;

    public ResultImportWriter(
        ILogger<ResultImportWriter> logger,
        ContestService contestService,
        PermissionService permissionService,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, Contest> contestRepo,
        ProportionalElectionResultImportWriter proportionalElectionResultImportWriter,
        MajorityElectionResultImportWriter majorityElectionResultImportWriter,
        SecondaryMajorityElectionResultImportWriter secondaryMajorityElectionResultImportWriter,
        VoteResultImportWriter voteResultImportWriter)
    {
        _logger = logger;
        _contestService = contestService;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _contestRepo = contestRepo;
        _proportionalElectionResultImportWriter = proportionalElectionResultImportWriter;
        _voteResultImportWriter = voteResultImportWriter;
        _secondaryMajorityElectionResultImportWriter = secondaryMajorityElectionResultImportWriter;
        _majorityElectionResultImportWriter = majorityElectionResultImportWriter;
        _permissionService = permissionService;
    }

    public async Task DeleteResults(Guid contestId)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var contest = await _contestRepo.Query()
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

        var countingCircleResultAggregatesToSave = await EnsureAllCountingCirclesInSubmissionOrCorrection(contest);

        var aggregate = _aggregateFactory.New<ResultImportAggregate>();
        aggregate.DeleteData(contestId);

        await CreateImportAggregateAndSetSuccessor(aggregate);

        foreach (var countingCircleResultAggregate in countingCircleResultAggregatesToSave)
        {
            await _aggregateRepository.Save(countingCircleResultAggregate);
        }

        await _aggregateRepository.Save(aggregate);
        _logger.LogInformation("Deleted imported data for contest {ContestId}", contestId);
    }

    public async Task MapMajorityElectionWriteIns(
        Guid importId,
        Guid electionId,
        Guid basisCountingCircleId,
        PoliticalBusinessType pbType,
        IReadOnlyCollection<MajorityElectionWriteIn> mappings)
    {
        _permissionService.EnsureErfassungElectionAdmin();
        var aggregate = await _aggregateRepository.GetById<ResultImportAggregate>(importId);

        await _permissionService.EnsureHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, aggregate.ContestId);
        await _contestService.EnsureNotLocked(aggregate.ContestId);

        switch (pbType)
        {
            case PoliticalBusinessType.MajorityElection:
                await _majorityElectionResultImportWriter.ValidateWriteIns(electionId, basisCountingCircleId, mappings);
                break;
            case PoliticalBusinessType.SecondaryMajorityElection:
                await _secondaryMajorityElectionResultImportWriter.ValidateWriteIns(electionId, basisCountingCircleId, mappings);
                break;
            default:
                throw new ValidationException("Write-Ins are only available for majority elections!");
        }

        aggregate.MapMajorityElectionWriteIns(electionId, basisCountingCircleId, pbType, mappings);
        await _aggregateRepository.Save(aggregate);
    }

    public Task Import(ResultImportMeta importMeta)
    {
        _permissionService.EnsureMonitoringElectionAdmin();

        var ech0222 = EchDeserializer.FromXml<Delivery>(importMeta.FileContent);
        var importData = Ech0222Deserializer.FromDelivery(ech0222);
        if (importMeta.ContestId != importData.ContestId)
        {
            throw new ValidationException("contestIds do not match");
        }

        return Import(importData, importMeta);
    }

    internal async Task Import(EVotingImport importData, ResultImportMeta importMeta)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.SimplePoliticalBusinesses)
            .Include(x => x.CountingCircleDetails)
            .ThenInclude(x => x.CountingCircle)
            .FirstOrDefaultAsync(x => x.Id == importData.ContestId)
            ?? throw new EntityNotFoundException(nameof(Contest), importMeta.ContestId);

        _permissionService.EnsureIsContestManager(contest);
        _contestService.EnsureNotLocked(contest);

        if (!contest.EVoting)
        {
            throw new EVotingNotActiveException(nameof(Contest), importMeta.ContestId);
        }

        ValidateAllCountingCirclesHaveEVoting(contest.CountingCircleDetails, importData.PoliticalBusinessResults);
        var resultsByType = GroupByBusinessType(importData.PoliticalBusinessResults, contest.SimplePoliticalBusinesses);

        var aggregate = _aggregateFactory.New<ResultImportAggregate>();
        aggregate.Start(importMeta.FileName, importData.ContestId, importData.EchMessageId);

        var countingCircleResultAggregatesToSave = await EnsureAllCountingCirclesInSubmissionOrCorrection(contest);

        foreach (var (pbType, results) in resultsByType)
        {
            switch (pbType)
            {
                case PoliticalBusinessType.Vote:
                    await ImportVote(importMeta, results, aggregate);
                    break;
                case PoliticalBusinessType.MajorityElection:
                    await ImportMajorityElection(importMeta, results, aggregate);
                    break;
                case PoliticalBusinessType.ProportionalElection:
                    await ImportProportionalElection(importMeta, results, aggregate);
                    break;
                case PoliticalBusinessType.SecondaryMajorityElection:
                    await ImportSecondaryMajorityElection(importMeta, results, aggregate);
                    break;
            }
        }

        aggregate.Complete();

        await CreateImportAggregateAndSetSuccessor(aggregate);
        foreach (var ccAggregate in countingCircleResultAggregatesToSave)
        {
            await _aggregateRepository.Save(ccAggregate);
        }

        await _aggregateRepository.Save(aggregate);
    }

    private Task<List<CountingCircleResultAggregate>> EnsureAllCountingCirclesInSubmissionOrCorrection(Contest contest)
    {
        var ccIds = contest.CountingCircleDetails
            .Where(x => x.EVoting)
            .Select(x => x.CountingCircleId)
            .ToList();
        return _majorityElectionResultImportWriter.EnsureAllCountingCirclesInSubmissionOrCorrection(contest.Id, ccIds)
            .Concat(_proportionalElectionResultImportWriter.EnsureAllCountingCirclesInSubmissionOrCorrection(contest.Id, ccIds))
            .Concat(_voteResultImportWriter.EnsureAllCountingCirclesInSubmissionOrCorrection(contest.Id, ccIds))
            .ToList();
    }

    private Dictionary<PoliticalBusinessType, List<EVotingPoliticalBusinessResult>> GroupByBusinessType(
        IEnumerable<EVotingPoliticalBusinessResult> importedPoliticalBusinessResults,
        IEnumerable<SimplePoliticalBusiness> simplePoliticalBusinesses)
    {
        var simplePoliticalBusinessesById = simplePoliticalBusinesses.ToDictionary(x => x.Id);

        var resultsByPoliticalBusinessId = importedPoliticalBusinessResults
            .GroupBy(x => x.PoliticalBusinessId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var resultsByBusinessType = new Dictionary<PoliticalBusinessType, List<EVotingPoliticalBusinessResult>>();
        foreach (var (politicalBusinessId, results) in resultsByPoliticalBusinessId)
        {
            var politicalBusinessType = GetPoliticalBusinessType(simplePoliticalBusinessesById, politicalBusinessId, results);

            if (!resultsByBusinessType.TryGetValue(politicalBusinessType, out var businessResults))
            {
                resultsByBusinessType[politicalBusinessType] = results;
            }
            else
            {
                businessResults.AddRange(results);
            }
        }

        return resultsByBusinessType;
    }

    private PoliticalBusinessType GetPoliticalBusinessType(
        Dictionary<Guid, SimplePoliticalBusiness> simplePoliticalBusinessesById,
        Guid politicalBusinessId,
        List<EVotingPoliticalBusinessResult> results)
    {
        // Special case for votes, as we are always 100% sure that they are votes (there are no subtypes as for elections)
        // Cannot resolve the votes by their political business ID, as the IDs may not match to an existing vote (only the ballot IDs match)
        if (results.All(r => r.PoliticalBusinessType == PoliticalBusinessType.Vote))
        {
            return PoliticalBusinessType.Vote;
        }

        if (!simplePoliticalBusinessesById.TryGetValue(politicalBusinessId, out var simplePoliticalBusiness))
        {
            throw new EntityNotFoundException(nameof(PoliticalBusiness), politicalBusinessId);
        }

        return simplePoliticalBusiness.PoliticalBusinessType;
    }

    private void ValidateAllCountingCirclesHaveEVoting(
        IEnumerable<ContestCountingCircleDetails> ccDetails,
        IEnumerable<EVotingPoliticalBusinessResult> importedCcResults)
    {
        var ccDetailsByBasisId = ccDetails.ToDictionary(x => x.CountingCircle.BasisCountingCircleId);
        foreach (var ccResult in importedCcResults)
        {
            if (!ccDetailsByBasisId.TryGetValue(ccResult.BasisCountingCircleId, out var ccDetail))
            {
                throw new EntityNotFoundException(nameof(CountingCircleResult), ccDetailsByBasisId);
            }

            if (!ccDetail.EVoting)
            {
                throw new EVotingNotActiveException(nameof(CountingCircle), ccResult.BasisCountingCircleId);
            }
        }
    }

    private async Task ImportVote(
        ResultImportMeta importMeta,
        IEnumerable<EVotingPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _voteResultImportWriter.BuildImports(importMeta.ContestId, results.Cast<EVotingVoteResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportVoteResult(data);
        }
    }

    private async Task ImportProportionalElection(
        ResultImportMeta importMeta,
        IEnumerable<EVotingPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _proportionalElectionResultImportWriter.BuildImports(importMeta.ContestId, results.Cast<EVotingElectionResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportProportionalElectionResult(data);
        }
    }

    private async Task ImportMajorityElection(
        ResultImportMeta importMeta,
        IEnumerable<EVotingPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _majorityElectionResultImportWriter.BuildImports(importMeta, results.Cast<EVotingElectionResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportMajorityElectionResult(data);
        }
    }

    private async Task ImportSecondaryMajorityElection(
        ResultImportMeta importMeta,
        IEnumerable<EVotingPoliticalBusinessResult> results,
        ResultImportAggregate aggregate)
    {
        var importData = _secondaryMajorityElectionResultImportWriter.BuildImports(importMeta, results.Cast<EVotingElectionResult>().ToList());
        await foreach (var data in importData)
        {
            aggregate.ImportSecondaryMajorityElectionResult(data);
        }
    }

    private async Task CreateImportAggregateAndSetSuccessor(ResultImportAggregate import)
    {
        var contestResultImports = await _aggregateRepository.GetOrCreateById<ContestResultImportsAggregate>(import.ContestId);
        var prevImportId = contestResultImports.LastImportId;

        contestResultImports.CreateImport(import.Id, import.ContestId);
        await _aggregateRepository.Save(contestResultImports);

        if (!prevImportId.HasValue)
        {
            return;
        }

        var prevImport = await _aggregateRepository.GetById<ResultImportAggregate>(prevImportId.Value);
        prevImport.SucceedBy(import.Id);
        await _aggregateRepository.Save(prevImport);
    }
}
