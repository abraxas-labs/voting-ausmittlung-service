// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Configuration;
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

namespace Voting.Ausmittlung.Core.Services.Write.Import;

public class ResultImportWriter
{
    private readonly ILogger<ResultImportWriter> _logger;
    private readonly ContestService _contestService;
    private readonly PermissionService _permissionService;
    private readonly IAggregateFactory _aggregateFactory;
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, ResultImport> _resultImportRepo;
    private readonly ProportionalElectionResultImportWriter _proportionalElectionResultImportWriter;
    private readonly MajorityElectionResultImportWriter _majorityElectionResultImportWriter;
    private readonly SecondaryMajorityElectionResultImportWriter _secondaryMajorityElectionResultImportWriter;
    private readonly VoteResultImportWriter _voteResultImportWriter;
    private readonly AppConfig _appConfig;
    private readonly Ech0110Deserializer _ech0110Deserializer;
    private readonly Ech0222Deserializer _ech0222Deserializer;

    public ResultImportWriter(
        ILogger<ResultImportWriter> logger,
        ContestService contestService,
        PermissionService permissionService,
        IAggregateFactory aggregateFactory,
        IAggregateRepository aggregateRepository,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, ResultImport> resultImportRepo,
        ProportionalElectionResultImportWriter proportionalElectionResultImportWriter,
        MajorityElectionResultImportWriter majorityElectionResultImportWriter,
        SecondaryMajorityElectionResultImportWriter secondaryMajorityElectionResultImportWriter,
        VoteResultImportWriter voteResultImportWriter,
        AppConfig appConfig,
        Ech0110Deserializer ech0110Deserializer,
        Ech0222Deserializer ech0222Deserializer)
    {
        _logger = logger;
        _contestService = contestService;
        _aggregateFactory = aggregateFactory;
        _aggregateRepository = aggregateRepository;
        _contestRepo = contestRepo;
        _resultImportRepo = resultImportRepo;
        _proportionalElectionResultImportWriter = proportionalElectionResultImportWriter;
        _voteResultImportWriter = voteResultImportWriter;
        _appConfig = appConfig;
        _secondaryMajorityElectionResultImportWriter = secondaryMajorityElectionResultImportWriter;
        _majorityElectionResultImportWriter = majorityElectionResultImportWriter;
        _permissionService = permissionService;
        _ech0110Deserializer = ech0110Deserializer;
        _ech0222Deserializer = ech0222Deserializer;
    }

    public async Task DeleteResults(Guid contestId)
    {
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
        var aggregate = await _aggregateRepository.GetById<ResultImportAggregate>(importId);

        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, aggregate.ContestId);
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

    public async Task ResetMajorityElectionWriteIns(
        Guid contestId,
        Guid basisCountingCircleId,
        Guid electionId,
        PoliticalBusinessType pbType)
    {
        await _permissionService.EnsureIsContestManagerAndInTestingPhaseOrHasPermissionsOnCountingCircleWithBasisId(basisCountingCircleId, contestId);
        await _contestService.EnsureNotLocked(contestId);

        var importId = await GetLatestResultImportId(contestId)
                       ?? throw new EntityNotFoundException(nameof(ResultImport), new { contestId });

        switch (pbType)
        {
            case PoliticalBusinessType.MajorityElection:
                await _majorityElectionResultImportWriter.ValidateState(electionId, basisCountingCircleId);
                break;
            case PoliticalBusinessType.SecondaryMajorityElection:
                await _secondaryMajorityElectionResultImportWriter.ValidateState(electionId, basisCountingCircleId);
                break;
            default:
                throw new ValidationException("Write-Ins are only available for majority elections!");
        }

        var aggregate = await _aggregateRepository.GetById<ResultImportAggregate>(importId);
        aggregate.ResetMajorityElectionWriteIns(electionId, basisCountingCircleId, pbType);
        await _aggregateRepository.Save(aggregate);
    }

    public async Task Import(ResultImportMeta importMeta, CancellationToken ct)
    {
        // VOTING-3558: must be fixed: cert pinning problems with malwarescanner. code commentet for futher reactivation
        // await _malwareScannerService.EnsureFileIsClean(importMeta.Ech0110FileContent, ct);
        // await _malwareScannerService.EnsureFileIsClean(importMeta.Ech0222FileContent, ct);
        // importMeta.Ech0222FileContent.Seek(0, SeekOrigin.Begin);
        var importData = _ech0222Deserializer.DeserializeXml(importMeta.Ech0222FileContent);
        if (importMeta.ContestId != importData.ContestId)
        {
            throw new ValidationException("contestIds do not match");
        }

        // importMeta.Ech0110FileContent.Seek(0, SeekOrigin.Begin);
        var (importedVotingCards, importedCountOfVotersInformations) = _ech0110Deserializer.DeserializeXml(importMeta.Ech0110FileContent);
        if (importMeta.ContestId != importedVotingCards.ContestId)
        {
            throw new ValidationException("contestIds do not match");
        }

        await Import(importData, importedVotingCards, importedCountOfVotersInformations, importMeta);
    }

    internal async Task Import(EVotingImport importData, EVotingVotingCardImport importedVotingCards, EVotingCountOfVotersInformationImport importedCountOfVotersInformations, ResultImportMeta importMeta)
    {
        var contest = await _contestRepo.Query()
            .AsSplitQuery()
            .Include(x => x.DomainOfInfluence)
            .Include(x => x.SimplePoliticalBusinesses)
            .Include(x => x.CountingCircleDetails)
            .ThenInclude(x => x.CountingCircle)
            .FirstOrDefaultAsync(x => x.Id == importData.ContestId)
            ?? throw new EntityNotFoundException(nameof(Contest), importData.ContestId);

        _permissionService.EnsureIsContestManager(contest);
        _contestService.EnsureNotLocked(contest);

        if (!contest.EVoting)
        {
            throw new EVotingNotActiveException(nameof(Contest), importMeta.ContestId);
        }

        var ignoredCountingCircles = ValidateAndFilterCountingCircles(
            contest,
            importData.PoliticalBusinessResults,
            importedVotingCards.CountingCircleVotingCards);

        MapCountOfVoterInformationsToResults(importedCountOfVotersInformations.CountingCircleResultsCountOfVotersInformations, importData.PoliticalBusinessResults);
        var resultsByType = GroupByBusinessType(importData.PoliticalBusinessResults, contest.SimplePoliticalBusinesses);

        // The file names and eCH message IDs are currently concatenated. In the future, both the results and voting cards should be provided
        // in one eCH file. In the past, only a single eCH-0222 file (without eCH-0110) was used.
        // We do not want to add a new eCH-0110 file name field to the event, as it would be needed only temporarily.
        var aggregate = _aggregateFactory.New<ResultImportAggregate>();
        aggregate.Start(
            $"{importMeta.Ech0222FileName} / {importMeta.Ech0110FileName}",
            importData.ContestId,
            $"{importData.EchMessageId} / {importedVotingCards.EchMessageId}",
            ignoredCountingCircles);

        var countingCircleResultAggregatesToSave = await EnsureAllCountingCirclesInSubmissionOrCorrection(contest);

        aggregate.ImportCountingCircleVotingCards(importedVotingCards.CountingCircleVotingCards);

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
        return _majorityElectionResultImportWriter.EnsureAllCountingCirclesInSubmissionOrCorrection(contest.Id, contest.TestingPhaseEnded, ccIds)
            .Concat(_proportionalElectionResultImportWriter.EnsureAllCountingCirclesInSubmissionOrCorrection(contest.Id, contest.TestingPhaseEnded, ccIds))
            .Concat(_voteResultImportWriter.EnsureAllCountingCirclesInSubmissionOrCorrection(contest.Id, contest.TestingPhaseEnded, ccIds))
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

    private IReadOnlyCollection<IgnoredImportCountingCircle> ValidateAndFilterCountingCircles(
        Contest contest,
        List<EVotingPoliticalBusinessResult> importedCcResults,
        List<EVotingCountingCircleVotingCards> importedVotingCards)
    {
        var testCountingCirclesById = _appConfig.Publisher.TestCountingCircles
            .GetValueOrDefault(contest.DomainOfInfluence.Canton, new List<TestCountingCircleConfig>())
            .ToDictionary(x => x.Id);
        var ignoredCountingCircles = new List<IgnoredImportCountingCircle>();

        var ccDetailsByBasisId = contest.CountingCircleDetails.ToDictionary(x => x.CountingCircle.BasisCountingCircleId);
        var importedVotingCardCcIds = importedVotingCards.Select(x => x.BasisCountingCircleId).ToHashSet();
        foreach (var ccResult in importedCcResults)
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
                continue;
            }

            if (!Guid.TryParse(ccResult.BasisCountingCircleId, out var basisCountingCircleId)
                || !ccDetailsByBasisId.TryGetValue(basisCountingCircleId, out var ccDetail))
            {
                _logger.LogWarning(
                    "Unknown counting circle ID {CountingCircleId} in result import provided, will be ignored",
                    ccResult.BasisCountingCircleId);
                ignoredCountingCircles.Add(new IgnoredImportCountingCircle
                {
                    CountingCircleId = ccResult.BasisCountingCircleId,
                });
                continue;
            }

            if (ccResult is EVotingEmptyResult)
            {
                _logger.LogWarning("Result of counting circle ID {CountingCircleId} is empty, will be ignored", ccResult.BasisCountingCircleId);
                ignoredCountingCircles.Add(new IgnoredImportCountingCircle
                {
                    CountingCircleId = ccResult.BasisCountingCircleId,
                });
                continue;
            }

            if (!ccDetail.EVoting)
            {
                throw new EVotingNotActiveException(nameof(CountingCircle), basisCountingCircleId);
            }

            if (!importedVotingCardCcIds.Contains(ccResult.BasisCountingCircleId))
            {
                throw new ValidationException($"Import does not contain voting cards for counting circle {ccResult.BasisCountingCircleId}");
            }
        }

        importedCcResults.RemoveAll(x => ignoredCountingCircles.Any(ignored => ignored.CountingCircleId == x.BasisCountingCircleId));
        importedVotingCards.RemoveAll(x => ignoredCountingCircles.Any(ignored => ignored.CountingCircleId == x.BasisCountingCircleId));

        if (importedCcResults.Count != importedCcResults.DistinctBy(x => (x.BasisCountingCircleId, x.PoliticalBusinessId)).Count())
        {
            throw new ValidationException("Duplicate counting circle results provided");
        }

        if (importedVotingCards.Count != importedVotingCards.DistinctBy(x => x.BasisCountingCircleId).Count())
        {
            throw new ValidationException("Duplicate counting circle voting cards provided");
        }

        var countOfImportedCountingCircles = importedCcResults.DistinctBy(x => x.BasisCountingCircleId).Count();
        if (countOfImportedCountingCircles != importedVotingCards.Count)
        {
            throw new ValidationException("Imported counting circles results do not match with voting cards");
        }

        if (contest.CountingCircleDetails.Count(x => x.EVoting) != countOfImportedCountingCircles)
        {
            throw new ValidationException("Did not provide results for all counting circles");
        }

        return ignoredCountingCircles.DistinctBy(x => x.CountingCircleId).ToList();
    }

    private void MapCountOfVoterInformationsToResults(List<EVotingCountingCircleResultCountOfVotersInformation> countOfVotersInformations, List<EVotingPoliticalBusinessResult> ccResults)
    {
        if (countOfVotersInformations.Count != countOfVotersInformations.DistinctBy(x => (x.BasisCountingCircleId, x.PoliticalBusinessId)).Count())
        {
            throw new ValidationException("Duplicate count of voters information provided");
        }

        foreach (var ccResult in ccResults)
        {
            ccResult.CountOfVotersInformation = countOfVotersInformations
                .Find(x => x.PoliticalBusinessId == ccResult.PoliticalBusinessId && x.BasisCountingCircleId == ccResult.BasisCountingCircleId)
                ?? throw new ValidationException($"Import does not contain count of voters information for counting circle {ccResult.BasisCountingCircleId} and political business {ccResult.PoliticalBusinessId}");
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

    private async Task<Guid?> GetLatestResultImportId(Guid contestId)
    {
        return await _resultImportRepo.Query()
            .Where(x => x.ContestId == contestId)
            .OrderByDescending(x => x.Started)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
    }
}
