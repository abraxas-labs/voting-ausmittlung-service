// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

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
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;
using ResultImportType = Voting.Ausmittlung.Data.Models.ResultImportType;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ResultImportProcessor :
    IEventProcessor<ResultImportStarted>,
    IEventProcessor<ResultImportCompleted>,
    IEventProcessor<ResultImportCountingCircleCompleted>,
    IEventProcessor<ResultImportDataDeleted>
{
    private readonly IDbRepository<DataContext, ResultImport> _importsRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly IDbRepository<DataContext, ContestCountingCircleDetails> _contestCountingCircleDetailsRepo;
    private readonly ProportionalElectionEndResultBuilder _proportionalElectionEndResultBuilder;
    private readonly ProportionalElectionResultBuilder _proportionalElectionResultBuilder;
    private readonly MajorityElectionEndResultBuilder _majorityElectionEndResultBuilder;
    private readonly MajorityElectionResultBuilder _majorityElectionResultBuilder;
    private readonly VoteEndResultBuilder _voteEndResultBuilder;
    private readonly VoteResultBuilder _voteResultBuilder;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;
    private readonly IMapper _mapper;
    private readonly EventLogger _eventLogger;

    public ResultImportProcessor(
        IMapper mapper,
        EventLogger eventLogger,
        IDbRepository<DataContext, ResultImport> importsRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        ProportionalElectionEndResultBuilder proportionalElectionEndResultBuilder,
        VoteEndResultBuilder voteEndResultBuilder,
        MajorityElectionEndResultBuilder majorityElectionEndResultBuilder,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder,
        ProportionalElectionResultBuilder proportionalElectionResultBuilder,
        MajorityElectionResultBuilder majorityElectionResultBuilder,
        VoteResultBuilder voteResultBuilder,
        IDbRepository<DataContext, ContestCountingCircleDetails> contestCountingCircleDetailsRepo)
    {
        _importsRepo = importsRepo;
        _contestRepo = contestRepo;
        _proportionalElectionEndResultBuilder = proportionalElectionEndResultBuilder;
        _voteEndResultBuilder = voteEndResultBuilder;
        _majorityElectionEndResultBuilder = majorityElectionEndResultBuilder;
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _proportionalElectionResultBuilder = proportionalElectionResultBuilder;
        _majorityElectionResultBuilder = majorityElectionResultBuilder;
        _voteResultBuilder = voteResultBuilder;
        _contestCountingCircleDetailsRepo = contestCountingCircleDetailsRepo;
    }

    public async Task Process(ResultImportStarted eventData)
    {
        var import = new ResultImport
        {
            Id = GuidParser.Parse(eventData.ImportId),
            Started = eventData.EventInfo.Timestamp.ToDateTime(),
            ContestId = GuidParser.Parse(eventData.ContestId),
            CountingCircleId = GuidParser.ParseNullable(eventData.CountingCircleId),
            StartedBy = eventData.EventInfo.User.ToDataUser(),
            FileName = eventData.FileName,
            IgnoredCountingCircles = _mapper.Map<List<IgnoredImportCountingCircle>>(eventData.IgnoredCountingCircles),
            EmptyCountingCircles = eventData.EmptyCountingCircleIds
                .Select(x => new EmptyImportCountingCircle
                {
                    CountingCircleId = GuidParser.Parse(x),
                }).ToList(),
            ImportType = (ResultImportType)eventData.ImportType,
        };
        import.FixImportType();

        if (import.CountingCircleId.HasValue)
        {
            import.CountingCircleId = AusmittlungUuidV5.BuildCountingCircleSnapshot(import.ContestId, import.CountingCircleId.Value);
        }

        await _importsRepo.Create(import);
        await DeleteImportedData(import);
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
            CountingCircleId = GuidParser.ParseNullable(eventData.CountingCircleId),
            StartedBy = eventData.EventInfo.User.ToDataUser(),
            ImportType = (ResultImportType)eventData.ImportType,
        };

        import.FixImportType();
        if (import.CountingCircleId.HasValue)
        {
            import.CountingCircleId = AusmittlungUuidV5.BuildCountingCircleSnapshot(import.ContestId, import.CountingCircleId.Value);
        }

        await _importsRepo.Create(import);
        await DeleteImportedData(import);
    }

    public async Task Process(ResultImportCompleted eventData)
    {
        var importId = GuidParser.Parse(eventData.ImportId);
        var import = await _importsRepo.GetByKey(importId)
                     ?? throw new EntityNotFoundException(nameof(ResultImport), importId);

        import.Completed = true;
        await _importsRepo.Update(import);
        await SetImported(import, true);

        _eventLogger.LogEvent(
            eventData,
            importId,
            importId,
            contestId: import.ContestId);
    }

    public Task Process(ResultImportCountingCircleCompleted eventData)
    {
        var importId = GuidParser.Parse(eventData.ImportId);
        var contestId = GuidParser.Parse(eventData.ContestId);
        var basisCountingCircleId = GuidParser.Parse(eventData.CountingCircleId);
        var importType = (ResultImportType)eventData.ImportType;
        var eventDetails = new ResultImportCountingCircleCompletedMessageDetail(importType, eventData.HasWriteIns);
        _eventLogger.LogEvent(
            eventData,
            importId,
            importId,
            contestId: contestId,
            basisCountingCircleId: basisCountingCircleId,
            details: new EventProcessedMessageDetails(CountingCircleImportCompleted: eventDetails));
        return Task.CompletedTask;
    }

    private async Task DeleteImportedData(ResultImport import)
    {
        switch (import.ImportType)
        {
            case ResultImportType.EVoting:
                await DeleteEVotingData(import);
                await SetEVotingImported(import, false);
                break;
            case ResultImportType.ECounting:
                await DeleteECountingData(import);
                await SetECountingImported(import, false);
                break;
        }
    }

    private async Task DeleteECountingData(ResultImport import)
    {
        await _proportionalElectionResultBuilder.ResetAllResults(import.ContestId, import.CountingCircleId!.Value, VotingDataSource.ECounting);
        await _majorityElectionResultBuilder.ResetAllResults(import.ContestId, import.CountingCircleId!.Value, VotingDataSource.ECounting);
        await _voteResultBuilder.ResetAllResults(import.ContestId, import.CountingCircleId!.Value, VotingDataSource.ECounting);
    }

    private async Task DeleteEVotingData(ResultImport import)
    {
        await _proportionalElectionEndResultBuilder.ResetAllResults(import.ContestId, VotingDataSource.EVoting);
        await _majorityElectionEndResultBuilder.ResetAllResults(import.ContestId, VotingDataSource.EVoting);
        await _voteEndResultBuilder.ResetAllResults(import.ContestId, VotingDataSource.EVoting);
        await _contestCountingCircleDetailsBuilder.ResetVotingCards(import.ContestId, null, VotingChannel.EVoting);
    }

    private async Task SetImported(ResultImport import, bool imported)
    {
        switch (import.ImportType)
        {
            case ResultImportType.EVoting:
                await SetEVotingImported(import, imported);
                break;
            case ResultImportType.ECounting:
                await SetECountingImported(import, imported);
                break;
        }
    }

    private async Task SetEVotingImported(ResultImport import, bool imported)
    {
        await _contestRepo.Query()
            .Where(x => x.Id == import.ContestId)
            .ExecuteUpdateAsync(x => x.SetProperty(c => c.EVotingResultsImported, imported));
    }

    private async Task SetECountingImported(ResultImport import, bool imported)
    {
        var wasImported = await _contestCountingCircleDetailsRepo.Query()
                              .Where(x => x.ContestId == import.ContestId && x.CountingCircleId == import.CountingCircleId)
                              .AnyAsync(x => x.ECountingResultsImported);
        if (wasImported == imported)
        {
            return;
        }

        var delta = imported ? 1 : -1;
        await _contestRepo.Query()
            .Where(x => x.Id == import.ContestId)
            .ExecuteUpdateAsync(x => x.SetProperty(
                c => c.NumberOfCountingCirclesWithECountingImported,
                c => c.NumberOfCountingCirclesWithECountingImported + delta));
        await _contestCountingCircleDetailsRepo.Query()
            .Where(x => x.ContestId == import.ContestId && x.CountingCircleId == import.CountingCircleId)
            .ExecuteUpdateAsync(x => x.SetProperty(c => c.ECountingResultsImported, imported));
    }
}
