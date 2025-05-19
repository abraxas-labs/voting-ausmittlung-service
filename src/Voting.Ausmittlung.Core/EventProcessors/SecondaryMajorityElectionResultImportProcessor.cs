// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class SecondaryMajorityElectionResultImportProcessor :
    IEventProcessor<SecondaryMajorityElectionResultImported>,
    IEventProcessor<SecondaryMajorityElectionWriteInBallotImported>,
    IEventProcessor<SecondaryMajorityElectionWriteInsMapped>,
    IEventProcessor<SecondaryMajorityElectionWriteInsReset>
{
    private readonly IDbRepository<DataContext, ResultImport> _importRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _secondaryMajorityElectionResultRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public SecondaryMajorityElectionResultImportProcessor(
        IDbRepository<DataContext, ResultImport> importRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionResult> secondaryMajorityElectionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        DataContext dataContext,
        IMapper mapper)
    {
        _importRepo = importRepo;
        _secondaryMajorityElectionResultRepo = secondaryMajorityElectionResultRepo;
        _simpleResultRepo = simpleResultRepo;
        _dataContext = dataContext;
        _mapper = mapper;
    }

    public async Task Process(SecondaryMajorityElectionResultImported eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // legacy events don't specify the import type but are all evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        // legacy events don't have the import id set
        var importId = GuidParser.ParseNullable(eventData.ImportId) ??
                       await GetLatestImportId(contestId, countingCircleId, importType);
        var dataSource = importType.GetDataSource();
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.PrimaryResult)
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings)
            .FirstOrDefaultAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElectionResult), new { countingCircleId, electionId });

        // no update for the count of voters, since we only know them per election group but not for each election itself.
        var subTotal = result.GetNonNullableSubTotal(dataSource);
        subTotal.InvalidVoteCount = eventData.InvalidVoteCount;
        subTotal.EmptyVoteCountExclWriteIns = eventData.EmptyVoteCount;
        subTotal.TotalCandidateVoteCountExclIndividual = eventData.TotalCandidateVoteCountExclIndividual;

        if (eventData.WriteIns.Count > 0)
        {
            result.PrimaryResult.AddElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result.PrimaryResult, importType);
        }

        ProcessCandidates(dataSource, result, eventData.CandidateResults);
        ProcessWriteIns(importId, importType, result, eventData.WriteIns);

        await _dataContext.SaveChangesAsync();
    }

    // Note: This event was not emitted in earlier versions of VOTING Ausmittlung.
    public async Task Process(SecondaryMajorityElectionWriteInBallotImported eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // legacy events don't specify the import type but are all evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        // legacy events don't have the import id set
        var importId = GuidParser.ParseNullable(eventData.ImportId) ??
                       await GetLatestImportId(contestId, countingCircleId, importType);

        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .FirstOrDefaultAsync(x =>
                x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElectionResult), new { countingCircleId, electionId });

        var writeInBallot = _mapper.Map<SecondaryMajorityElectionWriteInBallot>(eventData);
        writeInBallot.ImportId = importId;
        result.WriteInBallots.Add(writeInBallot);
        await _dataContext.SaveChangesAsync();
    }

    // Note: Write ins may be mapped multiple times, for example
    // a second event corrects the write in mapping from the first event, since something was mapped incorrectly
    public async Task Process(SecondaryMajorityElectionWriteInsMapped eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // legacy events don't specify the import type but are all evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        var dataSource = importType.GetDataSource();
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings.Where(y => y.ImportType == importType).OrderBy(m => m.WriteInCandidateName))
            .ThenInclude(x => x.CandidateResult)
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.BallotPositions)
            .ThenInclude(x => x.Ballot)
            .Include(x => x.PrimaryResult.MajorityElection.Contest.CantonDefaults)
            .FirstOrDefaultAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId
                    && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });
        var supportsInvalidVotes = result.PrimaryResult.MajorityElection.Contest.CantonDefaults.MajorityElectionInvalidVotes;

        // we remove this election from the count, we may re-add it later, if there are still unspecified write-ins
        if (result.WriteInMappings.HasUnspecifiedMappings(importType))
        {
            result.PrimaryResult.RemoveElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result.PrimaryResult, importType);
        }

        ApplyWriteInMappings(result, eventData, supportsInvalidVotes);

        if (result.WriteInMappings.HasUnspecifiedMappings(importType))
        {
            result.PrimaryResult.AddElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result.PrimaryResult, importType);
        }

        await _dataContext.SaveChangesAsync();
    }

    public async Task Process(SecondaryMajorityElectionWriteInsReset eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // legacy events don't specify the import type but are all evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        var dataSource = importType.GetDataSource();
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings.Where(y => y.ImportType == importType))
            .ThenInclude(x => x.CandidateResult)
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.BallotPositions)
            .ThenInclude(x => x.Ballot)
            .Include(x => x.PrimaryResult)
            .FirstOrDefaultAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId
                && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var hadUnmappedWriteIns = result.WriteInMappings.HasUnspecifiedMappings(importType);

        foreach (var writeInMapping in result.WriteInMappings)
        {
            ApplyWriteInMappingToResult(result, writeInMapping, -1);

            writeInMapping.CandidateResultId = null;
            writeInMapping.Target = MajorityElectionWriteInMappingTarget.Unspecified;

            foreach (var ballotPosition in writeInMapping.BallotPositions)
            {
                ballotPosition.Target = MajorityElectionWriteInMappingTarget.Unspecified;
            }
        }

        // WriteIns were correctly mapped before the reset -> only increase count in this case
        if (!hadUnmappedWriteIns)
        {
            result.PrimaryResult.AddElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result.PrimaryResult, importType);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void ApplyWriteInMappings(
        SecondaryMajorityElectionResult result,
        SecondaryMajorityElectionWriteInsMapped eventData,
        bool supportsInvalidVotes)
    {
        var candidateResultsByCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        var writeInsById = result.WriteInMappings.ToDictionary(x => x.Id);
        foreach (var updatedMapping in eventData.WriteInMappings)
        {
            if (!writeInsById.TryGetValue(GuidParser.Parse(updatedMapping.WriteInMappingId), out var existingMapping))
            {
                throw new ValidationException("Write in mapping not found");
            }

            // The write ins could already have been mapped, remove these vote counts from the result here
            ApplyWriteInMappingToResult(result, existingMapping, -1);

            // Update the target (ex. candidate, individual, ...) of the write in mappings
            UpdateExistingWithUpdatedMapping(existingMapping, updatedMapping, candidateResultsByCandidateId);
        }

        // Check whether the write ins lead to duplicated candidates on ballots and adjust these
        // Older events do not have write in ballot information and skip this logic
        foreach (var ballotPosition in result.WriteInMappings.SelectMany(m => m.BallotPositions))
        {
            UpdateBallotPosition(ballotPosition, supportsInvalidVotes);
        }

        foreach (var updatedMapping in eventData.WriteInMappings)
        {
            var existingMapping = writeInsById[GuidParser.Parse(updatedMapping.WriteInMappingId)];

            // Finally, add the updated write in mapping vote counts back to the result
            ApplyWriteInMappingToResult(result, existingMapping, 1);
        }
    }

    private void UpdateExistingWithUpdatedMapping(
        SecondaryMajorityElectionWriteInMapping existingMapping,
        MajorityElectionWriteInMappedEventData updatedMapping,
        Dictionary<Guid, SecondaryMajorityElectionCandidateResult> candidateResultsByCandidateId)
    {
        existingMapping.Target = _mapper.Map<MajorityElectionWriteInMappingTarget>(updatedMapping.Target);
        if (existingMapping.Target == MajorityElectionWriteInMappingTarget.Candidate)
        {
            var candidateId = GuidParser.Parse(updatedMapping.CandidateId);
            if (!candidateResultsByCandidateId.TryGetValue(candidateId, out var candidateResult))
            {
                throw new ValidationException("Candidate result for mapped write in not found");
            }

            existingMapping.CandidateResultId = candidateResult.Id;
            existingMapping.CandidateResult = candidateResult;
        }
        else
        {
            existingMapping.CandidateResultId = null;
        }

        foreach (var ballotPosition in existingMapping.BallotPositions)
        {
            ballotPosition.Target = existingMapping.Target;
        }
    }

    private void UpdateBallotPosition(SecondaryMajorityElectionWriteInBallotPosition ballotPosition, bool supportsInvalidVotes)
    {
        if (ballotPosition.Target != MajorityElectionWriteInMappingTarget.Candidate)
        {
            return;
        }

        var candidateId = ballotPosition.WriteInMapping!.CandidateId!.Value;
        if (ballotPosition.Ballot!.CandidateIds.Contains(candidateId)
            || ballotPosition.Ballot.WriteInPositions.Any(p =>
                p.Id != ballotPosition.Id
                && p.Target == MajorityElectionWriteInMappingTarget.Candidate
                && p.WriteInMapping!.CandidateId == candidateId))
        {
            ballotPosition.Target = supportsInvalidVotes
                ? MajorityElectionWriteInMappingTarget.Invalid
                : MajorityElectionWriteInMappingTarget.Empty;
        }
    }

    private void ApplyWriteInMappingToResult(
        SecondaryMajorityElectionResult result,
        SecondaryMajorityElectionWriteInMapping writeIn,
        int deltaFactor)
    {
        var dataSource = writeIn.ImportType.GetDataSource();
        var subTotal = result.GetNonNullableSubTotal(dataSource);

        // Needs to be handled separately, since ballot positions may not map to the candidate
        if (writeIn is { Target: MajorityElectionWriteInMappingTarget.Candidate, BallotPositions.Count: > 0 })
        {
            foreach (var position in writeIn.BallotPositions)
            {
                ApplyWriteInToResult(dataSource, subTotal, position.Target, writeIn.CandidateResult, deltaFactor);
            }

            return;
        }

        // Everything else can be directly applied via the aggregated write in mapping
        var deltaVoteCount = writeIn.VoteCount * deltaFactor;
        ApplyWriteInToResult(dataSource, subTotal, writeIn.Target, writeIn.CandidateResult, deltaVoteCount);
    }

    private void ApplyWriteInToResult(
        VotingDataSource dataSource,
        MajorityElectionResultSubTotal subTotal,
        MajorityElectionWriteInMappingTarget target,
        SecondaryMajorityElectionCandidateResult? candidateResult,
        int deltaVoteCount)
    {
        switch (target)
        {
            case MajorityElectionWriteInMappingTarget.Individual:
                subTotal.IndividualVoteCount += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Candidate:
                candidateResult!.AddWriteInsVoteCount(dataSource, deltaVoteCount);
                subTotal.TotalCandidateVoteCountExclIndividual += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Empty:
                subTotal.EmptyVoteCountWriteIns += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Invalid:
                subTotal.InvalidVoteCount += deltaVoteCount;
                break;
        }
    }

    private void ProcessWriteIns(
        Guid importId,
        ResultImportType importType,
        SecondaryMajorityElectionResult result,
        IEnumerable<MajorityElectionWriteInEventData> writeIns)
    {
        result.WriteInMappings.Clear();
        foreach (var writeIn in writeIns)
        {
            var newMapping = new SecondaryMajorityElectionWriteInMapping
            {
                Id = GuidParser.Parse(writeIn.WriteInMappingId),
                VoteCount = writeIn.VoteCount,
                WriteInCandidateName = writeIn.WriteInCandidateName,
                ImportType = importType,
                ImportId = importId,
            };
            result.WriteInMappings.Add(newMapping);

            // we need to set the added state explicitly
            // otherwise ef decides this based on the value of the primary key.
            _dataContext.Entry(newMapping).State = EntityState.Added;
        }
    }

    private void ProcessCandidates(
        VotingDataSource dataSource,
        SecondaryMajorityElectionResult result,
        IEnumerable<MajorityElectionCandidateResultImportEventData> importCandidateResults)
    {
        var byCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        foreach (var importCandidateResult in importCandidateResults)
        {
            var candidateResult = byCandidateId[GuidParser.Parse(importCandidateResult.CandidateId)];
            candidateResult.SetVoteCountExclWriteIns(dataSource, importCandidateResult.VoteCount);
        }
    }

    private async Task UpdateSimpleResult(MajorityElectionResult result, ResultImportType importType)
    {
        switch (importType)
        {
            case ResultImportType.EVoting:
                await _simpleResultRepo.Query()
                    .Where(x => x.Id == result.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(
                        y => y.CountOfElectionsWithUnmappedEVotingWriteIns,
                        result.CountOfElectionsWithUnmappedEVotingWriteIns));
                break;
            case ResultImportType.ECounting:
                await _simpleResultRepo.Query()
                    .Where(x => x.Id == result.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(
                        y => y.CountOfElectionsWithUnmappedECountingWriteIns,
                        result.CountOfElectionsWithUnmappedECountingWriteIns));
                break;
        }
    }

    private async Task<Guid> GetLatestImportId(Guid contestId, Guid? countingCircleId, ResultImportType type)
    {
        return await _importRepo.Query()
            .Where(x => x.ImportType == type && x.ContestId == contestId && x.CountingCircleId == countingCircleId)
            .OrderByDescending(x => x.Started)
            .Select(x => x.Id)
            .FirstAsync();
    }
}
