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
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using ResultImportType = Voting.Ausmittlung.Data.Models.ResultImportType;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionResultImportProcessor :
    IEventProcessor<MajorityElectionResultImported>,
    IEventProcessor<MajorityElectionWriteInBallotImported>,
    IEventProcessor<MajorityElectionWriteInsReset>,
    IEventProcessor<MajorityElectionWriteInsMapped>
{
    private readonly EventLogger _eventLogger;
    private readonly IDbRepository<DataContext, ResultImport> _importRepo;
    private readonly IDbRepository<DataContext, MajorityElectionResult> _majorityElectionResultRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public MajorityElectionResultImportProcessor(
        EventLogger eventLogger,
        IDbRepository<DataContext, ResultImport> importRepo,
        IDbRepository<DataContext, MajorityElectionResult> majorityElectionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        DataContext dataContext,
        IMapper mapper)
    {
        _eventLogger = eventLogger;
        _importRepo = importRepo;
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _simpleResultRepo = simpleResultRepo;
        _dataContext = dataContext;
        _mapper = mapper;
    }

    public async Task Process(MajorityElectionResultImported eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
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
        var result = await _majorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings.Where(y => y.ImportType == importType))
            .FirstOrDefaultAsync(x =>
                x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var subTotal = result.GetNonNullableSubTotal(dataSource);
        subTotal.InvalidVoteCount = eventData.InvalidVoteCount;
        subTotal.EmptyVoteCountExclWriteIns = eventData.EmptyVoteCount;
        subTotal.TotalCandidateVoteCountExclIndividual = eventData.TotalCandidateVoteCountExclIndividual;

        var countOfVoters = result.CountOfVoters.GetNonNullableSubTotal(dataSource);
        countOfVoters.ReceivedBallots = eventData.CountOfVoters;
        countOfVoters.BlankBallots = eventData.BlankBallotCount;
        countOfVoters.AccountedBallots = eventData.CountOfVoters - eventData.BlankBallotCount;

        if (importType == ResultImportType.EVoting)
        {
            result.TotalSentEVotingVotingCards = eventData.CountOfVotersInformation?.TotalCountOfVoters;
        }

        if (eventData.WriteIns.Count > 0)
        {
            result.AddElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result, importType);
        }

        result.UpdateVoterParticipation();

        ProcessCandidates(dataSource, result, eventData.CandidateResults);
        ProcessWriteIns(importId, importType, result, eventData.WriteIns);

        await _dataContext.SaveChangesAsync();
    }

    // Note: This event was not emitted in earlier versions of VOTING Ausmittlung.
    public async Task Process(MajorityElectionWriteInBallotImported eventData)
    {
        var contestId = GuidParser.Parse(eventData.ContestId);
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
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
        var result = await _majorityElectionResultRepo.Query()
            .AsTracking()
            .FirstOrDefaultAsync(x =>
                x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var writeInBallot = _mapper.Map<MajorityElectionWriteInBallot>(eventData);
        writeInBallot.ImportId = importId;
        result.WriteInBallots.Add(writeInBallot);
        await _dataContext.SaveChangesAsync();
    }

    public async Task Process(MajorityElectionWriteInsMapped eventData)
    {
        var importId = GuidParser.ParseNullable(eventData.ImportId); // was not present in earlier events
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // legacy events don't provide the import type.
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        var dataSource = importType.GetDataSource();
        var result = await _majorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings.Where(y => y.ImportType == importType).OrderBy(m => m.WriteInCandidateName))
            .ThenInclude(x => x.CandidateResult)
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.BallotPositions)
            .ThenInclude(x => x.Ballot)
            .Include(x => x.CountOfVoters)
            .Include(x => x.MajorityElection.Contest.CantonDefaults)
            .FirstOrDefaultAsync(x => x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });
        var supportsInvalidVotes = result.MajorityElection.Contest.CantonDefaults.MajorityElectionInvalidVotes;

        // we remove this election from the count, we may re-add it later, if there are still unspecified write-ins
        if (result.WriteInMappings.HasUnspecifiedMappings(importType))
        {
            result.RemoveElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result, importType);
        }

        // Write ins may be mapped multiple times, for example
        // a second event corrects the write in mapping from the first event, since something was mapped incorrectly
        var candidateResultsByCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        ResetWriteIns(result, candidateResultsByCandidateId);
        var (duplicatedCandidates, invalidDueToEmptyBallot) = ApplyWriteIns(result, eventData, candidateResultsByCandidateId, supportsInvalidVotes);

        if (result.WriteInMappings.HasUnspecifiedMappings(importType))
        {
            result.AddElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result, importType);
        }

        await _dataContext.SaveChangesAsync();

        var eventDetails = new WriteInsMappedMessageDetail(importType, result.Id, duplicatedCandidates, invalidDueToEmptyBallot);
        _eventLogger.LogEvent(
            eventData,
            importId ?? Guid.Empty,
            importId ?? Guid.Empty,
            politicalBusinessResultId: result.Id,
            details: new EventProcessedMessageDetails(WriteInsMapped: eventDetails));
    }

    public async Task Process(MajorityElectionWriteInsReset eventData)
    {
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;

        // legacy events don't specify the import type but are all evoting events
        if (importType == ResultImportType.Unspecified)
        {
            importType = ResultImportType.EVoting;
        }

        var dataSource = importType.GetDataSource();
        var result = await _majorityElectionResultRepo.Query()
             .AsTracking()
             .AsSplitQuery()
             .Include(x => x.CandidateResults)
             .Include(x => x.WriteInMappings.Where(y => y.ImportType == importType))
             .ThenInclude(x => x.CandidateResult)
             .Include(x => x.WriteInMappings)
             .ThenInclude(x => x.BallotPositions)
             .ThenInclude(x => x.Ballot)
             .Include(x => x.CountOfVoters)
             .Include(x => x.MajorityElection)
             .FirstOrDefaultAsync(x => x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var hadUnmappedWriteIns = result.WriteInMappings.HasUnspecifiedMappings(importType);
        var candidateResultsByCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        ResetWriteIns(result, candidateResultsByCandidateId);

        // WriteIns were correctly mapped before the reset -> only increase count in this case
        if (!hadUnmappedWriteIns)
        {
            result.AddElectionWithUnmappedWriteIns(dataSource);
            await UpdateSimpleResult(result, importType);
        }

        await _dataContext.SaveChangesAsync();
    }

    private void ResetWriteIns(MajorityElectionResult result, Dictionary<Guid, MajorityElectionCandidateResult> candidateResultsByCandidateId)
    {
        foreach (var writeInMapping in result.WriteInMappings)
        {
            ApplyWriteInMappingToResult(result, writeInMapping, candidateResultsByCandidateId, -1);

            writeInMapping.CandidateResultId = null;
            writeInMapping.CandidateResult = null;
            writeInMapping.Target = MajorityElectionWriteInMappingTarget.Unspecified;

            foreach (var ballotPosition in writeInMapping.BallotPositions)
            {
                ballotPosition.Target = MajorityElectionWriteInMappingTarget.Unspecified;
            }
        }
    }

    private (int DuplicatedCandidates, int InvalidDueToEmptyBallot) ApplyWriteIns(
        MajorityElectionResult result,
        MajorityElectionWriteInsMapped eventData,
        Dictionary<Guid, MajorityElectionCandidateResult> candidateResultsByCandidateId,
        bool supportsInvalidVotes)
    {
        int duplicatedCandidateCount = 0;
        int invalidBallotsDueToEmptyBallot = 0;

        var writeInsById = result.WriteInMappings.ToDictionary(x => x.Id);

        foreach (var updatedMapping in eventData.WriteInMappings)
        {
            if (!writeInsById.TryGetValue(GuidParser.Parse(updatedMapping.WriteInMappingId), out var mapping))
            {
                throw new ValidationException("Write in mapping not found");
            }

            mapping.Target = _mapper.Map<MajorityElectionWriteInMappingTarget>(updatedMapping.Target);
            if (mapping.Target == MajorityElectionWriteInMappingTarget.Candidate)
            {
                if (!candidateResultsByCandidateId.TryGetValue(GuidParser.Parse(updatedMapping.CandidateId), out var candidateResult))
                {
                    throw new ValidationException("Candidate result for mapped write in not found");
                }

                mapping.CandidateResultId = candidateResult.Id;
                mapping.CandidateResult = candidateResult;
            }

            foreach (var ballotPosition in mapping.BallotPositions)
            {
                ballotPosition.Target = mapping.Target;

                // Special case for canton St. Gallen: If applying the write ins causes a ballot to only consist of empty positions,
                // then the whole ballot counts as invalid ballot.
                // Other cantons do not get to this point, since they do not map write ins to empty positions and allow invalid votes.
                if (ballotPosition.Ballot!.AllPositionsEmpty() && !supportsInvalidVotes)
                {
                    ballotPosition.Target = MajorityElectionWriteInMappingTarget.InvalidBallot;
                    invalidBallotsDueToEmptyBallot++;
                }
            }
        }

        // Check whether the write ins lead to duplicated candidates on ballots and adjust these.
        // Older events do not have write in ballot information and skip this logic.
        foreach (var ballotPosition in result.WriteInMappings.SelectMany(m => m.BallotPositions))
        {
            if (ResolveDuplicateCandidatesOnSameBallot(ballotPosition, supportsInvalidVotes))
            {
                duplicatedCandidateCount++;
            }
        }

        foreach (var updatedMapping in eventData.WriteInMappings)
        {
            var existingMapping = writeInsById[GuidParser.Parse(updatedMapping.WriteInMappingId)];

            // Finally, add the updated write in mapping vote counts back to the result
            ApplyWriteInMappingToResult(result, existingMapping, candidateResultsByCandidateId, 1);
        }

        return (duplicatedCandidateCount, invalidBallotsDueToEmptyBallot);
    }

    private bool ResolveDuplicateCandidatesOnSameBallot(MajorityElectionWriteInBallotPosition ballotPosition, bool supportsInvalidVotes)
    {
        if (ballotPosition.Target != MajorityElectionWriteInMappingTarget.Candidate)
        {
            return false;
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
            return true;
        }

        return false;
    }

    private void ApplyWriteInMappingToResult(
        MajorityElectionResult result,
        MajorityElectionWriteInMapping writeIn,
        Dictionary<Guid, MajorityElectionCandidateResult> candidateResultsByCandidateId,
        int deltaFactor)
    {
        var dataSource = writeIn.ImportType.GetDataSource();
        var subTotal = result.GetNonNullableSubTotal(dataSource);

        // Old logic for past contests, which we need to keep around for backward compatibility
        if (writeIn.BallotPositions.Count == 0)
        {
            var deltaVoteCount = writeIn.VoteCount * deltaFactor;
            ApplyWriteInToResult(dataSource, subTotal, writeIn.Target, writeIn.CandidateResult, deltaVoteCount);
            return;
        }

        var countOfVoters = result.CountOfVoters.GetNonNullableSubTotal(dataSource);

        // With the newer logic, we need to examine each write in on each ballot separately
        foreach (var position in writeIn.BallotPositions)
        {
            if (position.Target == MajorityElectionWriteInMappingTarget.InvalidBallot
                && position == position.Ballot!.WriteInPositions.First(p => p.Target == MajorityElectionWriteInMappingTarget.InvalidBallot))
            {
                // The whole ballot counts as invalid, other write ins on the same ballot are not respected
                // Remove all the votes which were added temporarily to the result
                // Careful! Only do this once per ballot, since multiple ballot positions could map to an invalid ballot
                // Note for the future: A mapping to an invalid ballot should also mark secondary election ballots as invalid,
                // but this is not yet supported by the deliverer of the eCH file.
                // There, primary and secondary election results should be in the same ballot/ballotRawData, but this isn't the case yet.
                countOfVoters.InvalidBallots += deltaFactor;
                countOfVoters.AccountedBallots -= deltaFactor;

                subTotal.InvalidVoteCount -= position.Ballot!.InvalidVoteCount * deltaFactor;
                subTotal.EmptyVoteCountExclWriteIns -= position.Ballot!.EmptyVoteCount * deltaFactor;

                foreach (var candidateId in position.Ballot.CandidateIds)
                {
                    if (!candidateResultsByCandidateId.TryGetValue(candidateId, out var candidateResult))
                    {
                        throw new ValidationException("Candidate result not found");
                    }

                    candidateResult.AddVoteCountExclWriteIns(dataSource, -deltaFactor);
                    subTotal.TotalCandidateVoteCountExclIndividual -= deltaFactor;
                }

                continue;
            }

            // If the write in is placed on a ballot that is mapped to an invalid ballot, skip this position.
            // The whole ballot is invalid, positions with other targets do not count towards the result.
            if (position.Ballot!.MapsToInvalidBallot())
            {
                continue;
            }

            ApplyWriteInToResult(dataSource, subTotal, position.Target, writeIn.CandidateResult, deltaFactor);
        }
    }

    private void ApplyWriteInToResult(
        VotingDataSource dataSource,
        MajorityElectionResultSubTotal subTotal,
        MajorityElectionWriteInMappingTarget target,
        MajorityElectionCandidateResult? candidateResult,
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
        MajorityElectionResult result,
        IEnumerable<MajorityElectionWriteInEventData> writeIns)
    {
        result.WriteInMappings.Clear();
        foreach (var writeIn in writeIns)
        {
            var newMapping = new MajorityElectionWriteInMapping
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
        MajorityElectionResult result,
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
