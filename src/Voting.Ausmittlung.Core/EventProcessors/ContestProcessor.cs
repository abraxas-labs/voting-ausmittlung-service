// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Core.Utils.Snapshot;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ContestProcessor :
    IEventProcessor<ContestCreated>,
    IEventProcessor<ContestUpdated>,
    IEventProcessor<ContestDeleted>,
    IEventProcessor<ContestTestingPhaseEnded>,
    IEventProcessor<ContestPastLocked>,
    IEventProcessor<ContestPastUnlocked>,
    IEventProcessor<ContestArchived>
{
    private readonly ILogger<ContestProcessor> _logger;
    private readonly IDbRepository<DataContext, Contest> _repo;
    private readonly IDbRepository<DataContext, DomainOfInfluence> _doiRepo;
    private readonly ContestTranslationRepo _translationRepo;
    private readonly ContestSnapshotBuilder _contestSnapshotBuilder;
    private readonly ResultExportConfigurationRepo _resultExportConfigRepo;
    private readonly DomainOfInfluencePermissionBuilder _permissionBuilder;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;
    private readonly AggregatedContestCountingCircleDetailsBuilder _aggregatedContestCountingCircleDetailsBuilder;
    private readonly ResultExportConfigurationBuilder _resultExportConfigurationBuilder;
    private readonly IMapper _mapper;
    private readonly ContestResultInitializer _contestResultInitializer;
    private readonly ContestCantonDefaultsBuilder _contestCantonDefaultsBuilder;

    public ContestProcessor(
        ILogger<ContestProcessor> logger,
        IDbRepository<DataContext, Contest> repo,
        IDbRepository<DataContext, DomainOfInfluence> doiRepo,
        ContestTranslationRepo translationRepo,
        ContestSnapshotBuilder contestSnapshotBuilder,
        ResultExportConfigurationRepo resultExportConfigRepo,
        DomainOfInfluencePermissionBuilder permissionBuilder,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder,
        AggregatedContestCountingCircleDetailsBuilder aggregatedContestCountingCircleDetailsBuilder,
        ResultExportConfigurationBuilder resultExportConfigurationBuilder,
        IMapper mapper,
        ContestResultInitializer contestResultInitializer,
        ContestCantonDefaultsBuilder contestCantonDefaultsBuilder)
    {
        _logger = logger;
        _repo = repo;
        _doiRepo = doiRepo;
        _translationRepo = translationRepo;
        _contestSnapshotBuilder = contestSnapshotBuilder;
        _permissionBuilder = permissionBuilder;
        _mapper = mapper;
        _contestResultInitializer = contestResultInitializer;
        _contestCantonDefaultsBuilder = contestCantonDefaultsBuilder;
        _resultExportConfigRepo = resultExportConfigRepo;
        _aggregatedContestCountingCircleDetailsBuilder = aggregatedContestCountingCircleDetailsBuilder;
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
        _resultExportConfigurationBuilder = resultExportConfigurationBuilder;
    }

    public async Task Process(ContestCreated eventData)
    {
        var doiId = Guid.Parse(eventData.Contest.DomainOfInfluenceId);
        var doi = await _doiRepo.GetByKey(doiId)
            ?? throw new EntityNotFoundException(nameof(DomainOfInfluence), doiId);

        var contest = _mapper.Map<Contest>(eventData.Contest);
        await _contestCantonDefaultsBuilder.BuildForContest(contest, doi.Canton);
        await _repo.Create(contest);

        await _contestSnapshotBuilder.CreateSnapshotForContest(contest);
        await _contestCountingCircleDetailsBuilder.SyncAndResetEVoting(contest);
        await _permissionBuilder.RebuildPermissionTree();
        await _resultExportConfigurationBuilder.CreateResultExportConfigurationForContest(contest);
    }

    public async Task Process(ContestUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Contest.Id);

        var contest = await _repo.Query()
                                  .AsSplitQuery()
                                  .Include(x => x.CantonDefaults)
                                  .Include(x => x.CountingCircleDetails)
                                  .ThenInclude(x => x.CountOfVotersInformationSubTotals)
                                  .Include(x => x.CountingCircleDetails)
                                  .ThenInclude(x => x.VotingCards)
                                  .FirstOrDefaultAsync(x => x.Id == id)
                              ?? throw new EntityNotFoundException(id);

        var oldDoiId = contest.DomainOfInfluenceId;
        var oldEVoting = contest.EVoting;

        _mapper.Map(eventData.Contest, contest);

        contest.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(contest.Id, contest.DomainOfInfluenceId);
        await _translationRepo.DeleteRelatedTranslations(contest.Id);
        await _repo.Update(contest);

        if (oldEVoting != contest.EVoting || oldDoiId != contest.DomainOfInfluenceId)
        {
            var removed = await _contestCountingCircleDetailsBuilder.SyncAndResetEVoting(contest);
            await _aggregatedContestCountingCircleDetailsBuilder.AdjustAggregatedDetails(contest.Id, removed, true);
        }
    }

    public async Task Process(ContestDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ContestId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);
    }

    public async Task Process(ContestTestingPhaseEnded eventData)
    {
        var id = GuidParser.Parse(eventData.ContestId);

        var contest = await _repo.Query()
                          .AsSplitQuery()
                          .Include(x => x.Details)
                          .Include(x => x.CantonDefaults)
                          .FirstOrDefaultAsync(x => x.Id == id)
                      ?? throw new EntityNotFoundException(id);

        contest.State = ContestState.Active;
        contest.EVotingResultsImported = false;
        await _repo.Update(contest);

        await _contestResultInitializer.ResetContestResults(id, contest.Details?.Id);
        await _permissionBuilder.SetContestPermissionsFinal(id);

        _logger.LogInformation("Testing phase ended for contest {ContestId}", contest.Id);
    }

    public async Task Process(ContestPastLocked eventData)
    {
        var id = GuidParser.Parse(eventData.ContestId);
        await UpdateState(id, ContestState.PastLocked);
        await _resultExportConfigRepo.UnsetAllNextExecutionDates(id);
    }

    public Task Process(ContestPastUnlocked eventData) => UpdateState(GuidParser.Parse(eventData.ContestId), ContestState.PastUnlocked);

    public Task Process(ContestArchived eventData) => UpdateState(GuidParser.Parse(eventData.ContestId), ContestState.Archived);

    private async Task UpdateState(Guid id, ContestState newState)
    {
        var contest = await _repo.Query()
                .AsSplitQuery()
                .Include(x => x.CantonDefaults)
                .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        var oldState = contest.State;
        contest.State = newState;
        await _repo.Update(contest);

        _logger.LogInformation("Contest {ContestId} state changed from {OldState} to {NewState}", id, oldState, newState);
    }
}
