// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

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
    IEventProcessor<SecondaryMajorityElectionWriteInsMapped>
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
        result.EVotingSubTotal.EmptyVoteCount = eventData.EmptyVoteCount;
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

    public async Task Process(SecondaryMajorityElectionWriteInsMapped eventData)
    {
        var electionId = GuidParser.Parse(eventData.SecondaryMajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _secondaryMajorityElectionResultRepo.Query()
            .AsSplitQuery()
            .Include(x => x.CandidateResults)
            .Include(x => x.WriteInMappings)
            .ThenInclude(x => x.CandidateResult)
            .Include(x => x.PrimaryResult)
            .FirstOrDefaultAsync(x => x.PrimaryResult.CountingCircle.BasisCountingCircleId == countingCircleId
                    && x.SecondaryMajorityElectionId == electionId)
            ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        // we remove this election from the count, we may re-add it later, if there are still unspecified write-ins
        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.PrimaryResult.CountOfElectionsWithUnmappedWriteIns--;
            await UpdateSimpleResult(result.PrimaryResult);
        }

        var candidateResultsByCandidateId = result.CandidateResults.ToDictionary(x => x.CandidateId);
        var writeInsById = result.WriteInMappings.ToDictionary(x => x.Id);
        foreach (var writeInMapping in eventData.WriteInMappings)
        {
            if (!writeInsById.TryGetValue(GuidParser.Parse(writeInMapping.WriteInMappingId), out var resultMapping))
            {
                throw new ValidationException("Write in candidate not found");
            }

            AdjustWriteInMappingVoteCount(result, resultMapping, -1);

            resultMapping.Target = _mapper.Map<MajorityElectionWriteInMappingTarget>(writeInMapping.Target);
            if (resultMapping.Target == MajorityElectionWriteInMappingTarget.Candidate)
            {
                if (!candidateResultsByCandidateId.TryGetValue(GuidParser.Parse(writeInMapping.CandidateId), out var candidateResult))
                {
                    throw new ValidationException("Candidate result for mapped write in not found");
                }

                resultMapping.CandidateResultId = candidateResult.Id;
                resultMapping.CandidateResult = candidateResult;
            }
            else
            {
                resultMapping.CandidateResultId = null;
                resultMapping.CandidateResult = null;
            }

            AdjustWriteInMappingVoteCount(result, resultMapping, 1);
        }

        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.PrimaryResult.CountOfElectionsWithUnmappedWriteIns++;
            await UpdateSimpleResult(result.PrimaryResult);
        }

        await _secondaryMajorityElectionResultRepo.Update(result);
    }

    private void AdjustWriteInMappingVoteCount(
        SecondaryMajorityElectionResult result,
        SecondaryMajorityElectionWriteInMapping writeIn,
        int deltaFactor)
    {
        var deltaVoteCount = writeIn.VoteCount * deltaFactor;
        switch (writeIn.Target)
        {
            case MajorityElectionWriteInMappingTarget.Individual:
                result.EVotingSubTotal.IndividualVoteCount += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Candidate:
                writeIn.CandidateResult!.EVotingVoteCount += deltaVoteCount;
                result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual += deltaVoteCount;
                break;
            case MajorityElectionWriteInMappingTarget.Empty:
                result.EVotingSubTotal.EmptyVoteCount += deltaVoteCount;
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
            byCandidateId[GuidParser.Parse(importCandidateResult.CandidateId)].EVotingVoteCount = importCandidateResult.VoteCount;
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
