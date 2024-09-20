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
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionResult> _secondaryMajorityElectionResultRepo;
    private readonly IDbRepository<DataContext, SimpleCountingCircleResult> _simpleResultRepo;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public SecondaryMajorityElectionResultImportProcessor(
        IDbRepository<DataContext, SecondaryMajorityElectionResult> secondaryMajorityElectionResultRepo,
        IDbRepository<DataContext, SimpleCountingCircleResult> simpleResultRepo,
        DataContext dataContext,
        IMapper mapper)
    {
        _secondaryMajorityElectionResultRepo = secondaryMajorityElectionResultRepo;
        _simpleResultRepo = simpleResultRepo;
        _dataContext = dataContext;
        _mapper = mapper;
    }

    public async Task Process(SecondaryMajorityElectionResultImported eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.PrimaryResult)
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings)
            .FirstOrDefaultAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElectionResult), new { countingCircleId, electionId });

        // no update for the count of voters, since we only know them per election group but not for each election itself.
        result.EVotingSubTotal.InvalidVoteCount = eventData.InvalidVoteCount;
        result.EVotingSubTotal.EmptyVoteCountExclWriteIns = eventData.EmptyVoteCount;
        result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual = eventData.TotalCandidateVoteCountExclIndividual;

        if (eventData.WriteIns.Count > 0)
        {
            result.PrimaryResult.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result.PrimaryResult);
        }

        ProcessCandidates(result, eventData.CandidateResults);
        ProcessWriteIns(result, eventData.WriteIns);

        await _dataContext.SaveChangesAsync();
    }

    // Note: This event was not emitted in earlier versions of VOTING Ausmittlung.
    public async Task Process(SecondaryMajorityElectionWriteInBallotImported eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .FirstOrDefaultAsync(x =>
                x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(SecondaryMajorityElectionResult), new { countingCircleId, electionId });

        var writeInBallot = _mapper.Map<SecondaryMajorityElectionWriteInBallot>(eventData);
        result.WriteInBallots.Add(writeInBallot);
        await _dataContext.SaveChangesAsync();
    }

    // Note: Write ins may be mapped multiple times, for example
    // a second event corrects the write in mapping from the first event, since something was mapped incorrectly
    public async Task Process(SecondaryMajorityElectionWriteInsMapped eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings.OrderBy(m => m.WriteInCandidateName))
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
        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.PrimaryResult.CountOfElectionsWithUnmappedWriteIns--;
            await UpdateSimpleResult(result.PrimaryResult);
        }

        ApplyWriteInMappings(result, eventData, supportsInvalidVotes);

        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.PrimaryResult.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result.PrimaryResult);
        }

        await _dataContext.SaveChangesAsync();
    }

    public async Task Process(SecondaryMajorityElectionWriteInsReset eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsTracking()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.BallotPositions)
            .ThenInclude(x => x.Ballot)
            .Include(x => x.PrimaryResult)
            .FirstOrDefaultAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId
                && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        var hadUnmappedWriteIns = result.WriteInMappings.HasUnspecifiedMappings();

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
            result.PrimaryResult.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result.PrimaryResult);
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
        // Needs to be handled separately, since ballot positions may not map to the candidate
        if (writeIn.Target == MajorityElectionWriteInMappingTarget.Candidate && writeIn.BallotPositions.Count > 0)
        {
            foreach (var position in writeIn.BallotPositions)
            {
                ApplyWriteInToResult(result, position.Target, writeIn.CandidateResult, deltaFactor);
            }

            return;
        }

        // Everything else can be directly applied via the aggregated write in mapping
        var deltaVoteCount = writeIn.VoteCount * deltaFactor;
        ApplyWriteInToResult(result, writeIn.Target, writeIn.CandidateResult, deltaVoteCount);
    }

    private void ApplyWriteInToResult(
        SecondaryMajorityElectionResult result,
        MajorityElectionWriteInMappingTarget target,
        SecondaryMajorityElectionCandidateResult? candidateResult,
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
            };
            result.WriteInMappings.Add(newMapping);

            // we need to set the added state explicitly
            // otherwise ef decides this based on the value of the primary key.
            _dataContext.Entry(newMapping).State = EntityState.Added;
        }
    }

    private void ProcessCandidates(
        SecondaryMajorityElectionResult result,
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
