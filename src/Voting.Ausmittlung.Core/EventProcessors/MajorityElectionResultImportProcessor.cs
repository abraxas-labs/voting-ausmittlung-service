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

public class MajorityElectionResultImportProcessor :
    IEventProcessor<MajorityElectionResultImported>,
    IEventProcessor<MajorityElectionWriteInsMapped>
{
    private readonly IDbRepository<DataContext, MajorityElectionResult> _majorityElectionResultRepo;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;

    public MajorityElectionResultImportProcessor(
        IDbRepository<DataContext, MajorityElectionResult> majorityElectionResultRepo,
        DataContext dataContext,
        IMapper mapper)
    {
        _majorityElectionResultRepo = majorityElectionResultRepo;
        _dataContext = dataContext;
        _mapper = mapper;
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
        result.EVotingSubTotal.EmptyVoteCount = eventData.EmptyVoteCount;
        result.EVotingSubTotal.TotalCandidateVoteCountExclIndividual = eventData.TotalCandidateVoteCountExclIndividual;
        result.CountOfVoters.EVotingReceivedBallots = eventData.CountOfVoters;

        if (eventData.WriteIns.Count > 0)
        {
            result.CountOfElectionsWithUnmappedWriteIns++;
        }

        result.UpdateVoterParticipation();

        ProcessCandidates(result, eventData.CandidateResults);
        ProcessWriteIns(result, eventData.WriteIns);

        await _dataContext.SaveChangesAsync();
    }

    public async Task Process(MajorityElectionWriteInsMapped eventData)
    {
        var electionId = GuidParser.Parse(eventData.MajorityElectionId);
        var countingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var result = await _majorityElectionResultRepo.Query()
                         .AsSplitQuery()
                         .IgnoreQueryFilters() // load all translations (for the event log)
                         .Include(x => x.CandidateResults)
                         .Include(x => x.WriteInMappings)
                         .ThenInclude(x => x.CandidateResult)
                         .Include(x => x.MajorityElection.Translations) // needed for event log
                         .FirstOrDefaultAsync(x =>
                             x.CountingCircle.BasisCountingCircleId == countingCircleId && x.MajorityElectionId == electionId)
                     ?? throw new EntityNotFoundException(nameof(MajorityElectionResult), new { countingCircleId, electionId });

        // we remove this election from the count, we may re-add it later, if there are still unspecified write-ins
        if (result.WriteInMappings.HasUnspecifiedMappings())
        {
            result.CountOfElectionsWithUnmappedWriteIns--;
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
            result.CountOfElectionsWithUnmappedWriteIns++;
        }

        await _majorityElectionResultRepo.Update(result);
    }

    private void AdjustWriteInMappingVoteCount(
        MajorityElectionResult result,
        MajorityElectionWriteInMapping writeIn,
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
            byCandidateId[GuidParser.Parse(importCandidateResult.CandidateId)].EVotingVoteCount = importCandidateResult.VoteCount;
        }
    }
}
