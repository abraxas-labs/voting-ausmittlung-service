// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Messaging.Messages;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using Voting.Lib.Messaging;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ResultImportProcessor :
    IEventProcessor<ResultImportStarted>,
    IEventProcessor<ResultImportCompleted>,
    IEventProcessor<ResultImportDataDeleted>
{
    private readonly IDbRepository<DataContext, ResultImport> _importsRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, MajorityElectionWriteInMapping> _majorityWriteInMappingRepo;
    private readonly IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> _secondaryMajorityWriteInMappingRepo;
    private readonly ProportionalElectionEndResultBuilder _proportionalElectionEndResultBuilder;
    private readonly MajorityElectionEndResultBuilder _majorityElectionEndResultBuilder;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;
    private readonly IMapper _mapper;
    private readonly VoteEndResultBuilder _voteEndResultBuilder;
    private readonly MessageProducerBuffer _resultImportChangeMessageProducerBuffer;

    public ResultImportProcessor(
        IDbRepository<DataContext, ResultImport> importsRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        IDbRepository<DataContext, MajorityElectionWriteInMapping> majorityWriteInMappingRepo,
        IDbRepository<DataContext, SecondaryMajorityElectionWriteInMapping> secondaryMajorityWriteInMappingRepo,
        ProportionalElectionEndResultBuilder proportionalElectionEndResultBuilder,
        VoteEndResultBuilder voteEndResultBuilder,
        MajorityElectionEndResultBuilder majorityElectionEndResultBuilder,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder,
        MessageProducerBuffer resultImportChangeMessageProducerBuffer,
        IMapper mapper)
    {
        _importsRepo = importsRepo;
        _contestRepo = contestRepo;
        _majorityWriteInMappingRepo = majorityWriteInMappingRepo;
        _secondaryMajorityWriteInMappingRepo = secondaryMajorityWriteInMappingRepo;
        _proportionalElectionEndResultBuilder = proportionalElectionEndResultBuilder;
        _voteEndResultBuilder = voteEndResultBuilder;
        _majorityElectionEndResultBuilder = majorityElectionEndResultBuilder;
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
        _mapper = mapper;
        _resultImportChangeMessageProducerBuffer = resultImportChangeMessageProducerBuffer;
    }

    public async Task Process(ResultImportStarted eventData)
    {
        var import = new ResultImport
        {
            Id = GuidParser.Parse(eventData.ImportId),
            Started = eventData.EventInfo.Timestamp.ToDateTime(),
            ContestId = GuidParser.Parse(eventData.ContestId),
            StartedBy = eventData.EventInfo.User.ToDataUser(),
            FileName = eventData.FileName,
            IgnoredCountingCircles = _mapper.Map<List<IgnoredImportCountingCircle>>(eventData.IgnoredCountingCircles),
        };

        await _importsRepo.Create(import);
        await DeleteEVotingData(import.ContestId);
    }

    public async Task Process(ResultImportDataDeleted eventData)
    {
        var import = new ResultImport
        {
            Id = GuidParser.Parse(eventData.ImportId),
            Started = eventData.EventInfo.Timestamp.ToDateTime(),
            Deleted = true,
            Completed = true,
            ContestId = GuidParser.Parse(eventData.ContestId),
            StartedBy = eventData.EventInfo.User.ToDataUser(),
        };

        await _importsRepo.Create(import);
        await DeleteEVotingData(import.ContestId);
    }

    public async Task Process(ResultImportCompleted eventData)
    {
        var importId = GuidParser.Parse(eventData.ImportId);
        var import = await _importsRepo.GetByKey(importId)
                     ?? throw new EntityNotFoundException(nameof(ResultImport), importId);

        import.Completed = true;
        await _importsRepo.Update(import);
        await SetContestEVotingImported(import.ContestId, true);

        var countingCircleIdsWithMajorityElectionWriteIns = await _majorityWriteInMappingRepo.Query()
            .Include(x => x.Result.CountingCircle)
            .Where(x =>
                x.Result.CountingCircle.SnapshotContestId == import.ContestId &&
                eventData.ImportedMajorityElectionIds.Contains(x.Result.MajorityElectionId.ToString()))
            .Select(x => x.Result.CountingCircle.BasisCountingCircleId)
            .Distinct()
            .ToListAsync();

        var countingCircleIdsWithSecondaryMajorityElectionWriteIns = await _secondaryMajorityWriteInMappingRepo.Query()
            .Include(x => x.Result.PrimaryResult.CountingCircle)
            .Where(x =>
                x.Result.PrimaryResult.CountingCircle.SnapshotContestId == import.ContestId &&
                eventData.ImportedSecondaryMajorityElectionIds.Contains(x.Result.SecondaryMajorityElectionId.ToString()))
            .Select(x => x.Result.PrimaryResult.CountingCircle.BasisCountingCircleId)
            .Distinct()
            .ToListAsync();

        var countingCircleIdsWithWriteIns = countingCircleIdsWithMajorityElectionWriteIns
            .Concat(countingCircleIdsWithSecondaryMajorityElectionWriteIns)
            .ToHashSet();

        var contest = await _contestRepo.Query()
            .Include(x => x.CountingCircleDetails)
            .ThenInclude(x => x.CountingCircle)
            .FirstOrDefaultAsync(x => x.Id == import.ContestId)
            ?? throw new EntityNotFoundException(nameof(Contest), import.ContestId);

        foreach (var ccDetails in contest.CountingCircleDetails)
        {
            var hasWriteIns = countingCircleIdsWithWriteIns.Contains(ccDetails.CountingCircle.BasisCountingCircleId);
            _resultImportChangeMessageProducerBuffer.Add(new ResultImportChanged(contest.Id, ccDetails.CountingCircle.BasisCountingCircleId, hasWriteIns));
        }
    }

    private async Task DeleteEVotingData(Guid contestId)
    {
        await SetContestEVotingImported(contestId, false);
        await _proportionalElectionEndResultBuilder.ResetAllResults(contestId, VotingDataSource.EVoting);
        await _majorityElectionEndResultBuilder.ResetAllResults(contestId, VotingDataSource.EVoting);
        await _voteEndResultBuilder.ResetAllResults(contestId, VotingDataSource.EVoting);
        await _contestCountingCircleDetailsBuilder.ResetEVotingVotingCards(contestId);
    }

    private async Task SetContestEVotingImported(Guid contestId, bool imported)
    {
        var contest = await _contestRepo.GetByKey(contestId)
                      ?? throw new EntityNotFoundException(nameof(Contest), contestId);
        contest.EVotingResultsImported = imported;
        await _contestRepo.Update(contest);
    }
}
