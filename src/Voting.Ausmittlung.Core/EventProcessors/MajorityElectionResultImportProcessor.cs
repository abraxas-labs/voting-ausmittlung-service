// (c) Copyright 2024 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionResultImportProcessor :
    IEventProcessor<MajorityElectionResultImported>,
    IEventProcessor<MajorityElectionWriteInBallotImported>,
    IEventProcessor<MajorityElectionWriteInsReset>,
    IEventProcessor<MajorityElectionWriteInsMapped>
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _majorityElectionResultRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;
    private readonly MessageProducerBuffer _messageProducerBuffer;

    public MajorityElectionResultImportProcessor(
        IDbRepository<DataContext, MajorityElectionResult> majorityElectionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        DataContext dataContext,
        IMapper mapper,
        MessageProducerBuffer messageProducerBuffer)
    {
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _simpleResultRepo = simpleResultRepo;
        _dataContext = dataContext;
        _mapper = mapper;
        _messageProducerBuffer = messageProducerBuffer;
    }

    public async Task Process(MajorityElectionResultImported eventData)
    {
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _majorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings)
            .FirstOrDefaultAsync(x =>
                x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        result.EVotingSubTotal.InvalidVoteCount = eventData.InvalidVoteCount;
        result.EVotingSubTotal.EmptyVoteCountExclWriteIns = eventData.EmptyVoteCount;
        result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual = eventData.TotalCandidateVoteCountExclIndividual;
        result.CountOfVoters.EVotingReceivedBallots = eventData.CountOfVoters;
        result.CountOfVoters.EVotingBlankBallots = eventData.BlankBallotCount;
        result.CountOfVoters.EVotingAccountedBallots = eventData.CountOfVoters - eventData.BlankBallotCount;
        result.TotalSentEVotingVotingCards = eventData.CountOfVotersInformation?.TotalCountOfVoters;

        if (eventData.WriteIns.Count > 0)
        {
            result.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result);
        }

        result.UpdateVoterParticipation();

        ProcessCandidates(result, eventData.CandidateResults);
        ProcessWriteIns(result, eventData.WriteIns);

        await _dataContext.SaveChangesAsync();
    }

    // Note: This event was not emitted in earlier versions of VOTING Ausmittlung.
    public async Task Process(MajorityElectionWriteInBallotImported eventData)
    {
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _majorityElectionResultRepo.Query()
            .AsTracking()
            .FirstOrDefaultAsync(x =>
                x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var writeInBallot = _mapper.Map<MajorityElectionWriteInBallot>(eventData);
        result.WriteInBallots.Add(writeInBallot);
        await _dataContext.SaveChangesAsync();
    }

    public async Task Process(MajorityElectionWriteInsMapped eventData)
    {
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _majorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings.OrderBy(m => m.WriteInCandidateName))
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
        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.CountOfElectionsWithUnmappedWriteIns--;
            await UpdateSimpleResult(result);
        }

        // Write ins may be mapped multiple times, for example
        // a second event corrects the write in mapping from the first event, since something was mapped incorrectly
        var candidateResultsByCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        ResetWriteIns(result, candidateResultsByCandidateId);
        var (duplicatedCandidates, invalidDueToEmptyBallot) = ApplyWriteIns(result, eventData, candidateResultsByCandidateId, supportsInvalidVotes);

        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result);
        }

        await _dataContext.SaveChangesAsync();

        _messageProducerBuffer.Add(new WriteInMappingsChanged(result.Id, false, duplicatedCandidates, invalidDueToEmptyBallot));
    }

    public async Task Process(MajorityElectionWriteInsReset eventData)
    {
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _majorityElectionResultRepo.Query()
             .AsTracking()
             .AsSplitQuery()
             .Include(x => x.CandidateResults)
             .Include(x => x.WriteInMappings)
             .ThenInclude(x => x.CandidateResult)
             .Include(x => x.WriteInMappings)
             .ThenInclude(x => x.BallotPositions)
             .ThenInclude(x => x.Ballot)
             .Include(x => x.CountOfVoters)
             .Include(x => x.MajorityElection)
             .FirstOrDefaultAsync(x => x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var hadUnmappedWriteIns = result.WriteInMappings.HasUnspecifiedMappings();
        var candidateResultsByCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        ResetWriteIns(result, candidateResultsByCandidateId);

        // WriteIns were correctly mapped before the reset -> only increase count in this case
        if (!hadUnmappedWriteIns)
        {
            result.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result);
        }

        await _dataContext.SaveChangesAsync();

        _messageProducerBuffer.Add(new WriteInMappingsChanged(result.Id, true, 0, 0));
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
        // Old logic for past contests, which we need to keep around for backward compatibility
        if (writeIn.BallotPositions.Count == 0)
        {
            var deltaVoteCount = writeIn.VoteCount * deltaFactor;
            ApplyWriteInToResult(result, writeIn.Target, writeIn.CandidateResult, deltaVoteCount);
            return;
        }

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
                result.CountOfVoters.EVotingInvalidBallots += deltaFactor;
                result.CountOfVoters.EVotingAccountedBallots -= deltaFactor;

                result.EVotingSubTotal.InvalidVoteCount -= position.Ballot!.InvalidVoteCount * deltaFactor;
                result.EVotingSubTotal.EmptyVoteCountExclWriteIns -= position.Ballot!.EmptyVoteCount * deltaFactor;

                foreach (var candidateId in position.Ballot.CandidateIds)
                {
                    if (!candidateResultsByCandidateId.TryGetValue(candidateId, out var candidateResult))
                    {
                        throw new ValidationException("Candidate result not found");
                    }

                    candidateResult.EVotingExclWriteInsVoteCount -= deltaFactor;
                    result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual -= deltaFactor;
                }

                continue;
            }

            // If the write in is placed on a ballot that is mapped to an invalid ballot, skip this position.
            // The whole ballot is invalid, positions with other targets do not count towards the result.
            if (position.Ballot!.MapsToInvalidBallot())
            {
                continue;
            }

            ApplyWriteInToResult(result, position.Target, writeIn.CandidateResult, deltaFactor);
        }
    }

    private void ApplyWriteInToResult(
        MajorityElectionResult result,
        MajorityElectionWriteInMappingTarget target,
        MajorityElectionCandidateResult? candidateResult,
        int deltaVoteCount)
    {
        switch (target)
        {
            case MajorityElectionWriteInMappingTarget.Individual:
                result.EVotingSubTotal.IndividualVoteCount += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Candidate:
                candidateResult!.EVotingWriteInsVoteCount += deltaVoteCount;
                result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Empty:
                result.EVotingSubTotal.EmptyVoteCountWriteIns += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Invalid:
                result.EVotingSubTotal.InvalidVoteCount += deltaVoteCount;
                break;
        }
    }

    private void ProcessWriteIns(
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
            };
            result.WriteInMappings.Add(newMapping);

            // we need to set the added state explicitly
            // otherwise ef decides this based on the value of the primary key.
            _dataContext.Entry(newMapping).State = EntityState.Added;
        }
    }

    private void ProcessCandidates(
        MajorityElectionResult result,
        IEnumerable<MajorityElectionCandidateResultImportEventData> importCandidateResults)
    {
        var byCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        foreach (var importCandidateResult in importCandidateResults)
        {
            byCandidateId[GuidParser.Parse(importCandidateResult.CandidateId)].EVotingExclWriteInsVoteCount = importCandidateResult.VoteCount;
        }
    }

    private async Task UpdateSimpleResult(MajorityElectionResult result)
    {
        var simpleResult = await _simpleResultRepo.GetByKey(result.Id)
            ?? throw new EntityNotFoundException(nameof(SimpleCountingCircleResult), result.Id);

        simpleResult.CountOfElectionsWithUnmappedWriteIns = result.CountOfElectionsWithUnmappedWriteIns;
        await _simpleResultRepo.Update(simpleResult);
    }
}
