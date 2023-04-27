// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Extensions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ResultImportProcessor :
    IEventProcessor<ResultImportStarted>,
    IEventProcessor<ResultImportCompleted>,
    IEventProcessor<ResultImportDataDeleted>
{
    private readonly IDbRepository<DataContext, ResultImport> _importsRepo;
    private readonly IDbRepository<DataContext, Contest> _contestRepo;
    private readonly ProportionalElectionEndResultBuilder _proportionalElectionEndResultBuilder;
    private readonly MajorityElectionEndResultBuilder _majorityElectionEndResultBuilder;
    private readonly IMapper _mapper;
    private readonly VoteEndResultBuilder _voteEndResultBuilder;

    public ResultImportProcessor(
        IDbRepository<DataContext, ResultImport> importsRepo,
        IDbRepository<DataContext, Contest> contestRepo,
        ProportionalElectionEndResultBuilder proportionalElectionEndResultBuilder,
        VoteEndResultBuilder voteEndResultBuilder,
        MajorityElectionEndResultBuilder majorityElectionEndResultBuilder,
        IMapper mapper)
    {
        _importsRepo = importsRepo;
        _contestRepo = contestRepo;
        _proportionalElectionEndResultBuilder = proportionalElectionEndResultBuilder;
        _voteEndResultBuilder = voteEndResultBuilder;
        _majorityElectionEndResultBuilder = majorityElectionEndResultBuilder;
        _mapper = mapper;
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
    }

    private async Task DeleteEVotingData(Guid contestId)
    {
        await SetContestEVotingImported(contestId, false);
        await _proportionalElectionEndResultBuilder.ResetAllResults(contestId, VotingDataSource.EVoting);
        await _majorityElectionEndResultBuilder.ResetAllResults(contestId, VotingDataSource.EVoting);
        await _voteEndResultBuilder.ResetAllResults(contestId, VotingDataSource.EVoting);
    }

    private async Task SetContestEVotingImported(Guid contestId, bool imported)
    {
        var contest = await _contestRepo.GetByKey(contestId)
                      ?? throw new EntityNotFoundException(nameof(Contest), contestId);
        contest.EVotingResultsImported = imported;
        await _contestRepo.Update(contest);
    }
}
