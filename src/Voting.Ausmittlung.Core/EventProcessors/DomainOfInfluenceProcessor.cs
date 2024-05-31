// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Queries;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class DomainOfInfluenceProcessor :
    IEventProcessor<DomainOfInfluenceCreated>,
    IEventProcessor<DomainOfInfluenceUpdated>,
    IEventProcessor<DomainOfInfluenceDeleted>,
    IEventProcessor<DomainOfInfluenceCountingCircleEntriesUpdated>,
    IEventProcessor<DomainOfInfluenceContactPersonUpdated>,
    IEventProcessor<DomainOfInfluencePlausibilisationConfigurationUpdated>,
    IEventProcessor<DomainOfInfluencePartyCreated>,
    IEventProcessor<DomainOfInfluencePartyUpdated>,
    IEventProcessor<DomainOfInfluencePartyDeleted>
{
    private readonly DomainOfInfluenceRepo _repo;
    private readonly DomainOfInfluencePermissionBuilder _permissionBuilder;
    private readonly ContestRepo _contestRepo;
    private readonly CountingCircleRepo _countingCircleRepo;
    private readonly DomainOfInfluencePartyRepo _doiPartyRepo;
    private readonly DomainOfInfluenceCountingCircleInheritanceBuilder _doiCcInheritanceBuilder;
    private readonly ContestCountingCircleDetailsBuilder _contestCountingCircleDetailsBuilder;
    private readonly IMapper _mapper;
    private readonly DomainOfInfluenceCantonDefaultsBuilder _domainOfInfluenceCantonDefaultsBuilder;
    private readonly CountingCircleResultsInitializer _ccResultsInitializer;
    private readonly DataContext _db;

    public DomainOfInfluenceProcessor(
        DomainOfInfluenceRepo repo,
        DomainOfInfluencePermissionBuilder permissionBuilder,
        ContestRepo contestRepo,
        CountingCircleRepo countingCircleRepo,
        DomainOfInfluencePartyRepo doiPartyRepo,
        DomainOfInfluenceCountingCircleInheritanceBuilder doiCcInheritanceBuilder,
        ContestCountingCircleDetailsBuilder contestCountingCircleDetailsBuilder,
        DomainOfInfluenceCantonDefaultsBuilder domainOfInfluenceCantonDefaultsBuilder,
        IMapper mapper,
        CountingCircleResultsInitializer ccResultsInitializer,
        DataContext db)
    {
        _repo = repo;
        _permissionBuilder = permissionBuilder;
        _contestRepo = contestRepo;
        _countingCircleRepo = countingCircleRepo;
        _doiPartyRepo = doiPartyRepo;
        _doiCcInheritanceBuilder = doiCcInheritanceBuilder;
        _mapper = mapper;
        _contestCountingCircleDetailsBuilder = contestCountingCircleDetailsBuilder;
        _domainOfInfluenceCantonDefaultsBuilder = domainOfInfluenceCantonDefaultsBuilder;
        _ccResultsInitializer = ccResultsInitializer;
        _db = db;
    }

    public async Task Process(DomainOfInfluenceCreated eventData)
    {
        var domainOfInfluence = _mapper.Map<DomainOfInfluence>(eventData.DomainOfInfluence);

        if (domainOfInfluence.ParentId != null)
        {
            domainOfInfluence.Canton = await _repo.GetRootCanton(domainOfInfluence.ParentId.Value);
        }

        await _domainOfInfluenceCantonDefaultsBuilder.BuildForDomainOfInfluence(domainOfInfluence);
        await _repo.Create(domainOfInfluence);

        var contestInTestingPhase = await _contestRepo.GetContestsInTestingPhase();
        foreach (var contest in contestInTestingPhase)
        {
            var snapshotDomainOfInfluence = _mapper.Map<DomainOfInfluence>(eventData.DomainOfInfluence);
            snapshotDomainOfInfluence.Canton = domainOfInfluence.Canton;
            snapshotDomainOfInfluence.SnapshotForContest(contest.Id);

            var parentId = snapshotDomainOfInfluence.ParentId;
            if (parentId != null)
            {
                snapshotDomainOfInfluence.ParentId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(contest.Id, parentId.Value);
            }

            await _repo.Create(snapshotDomainOfInfluence);
        }

        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(DomainOfInfluenceUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluence.Id);
        var domainOfInfluence = _mapper.Map<DomainOfInfluence>(eventData.DomainOfInfluence);
        var isRootDomainOfInfluence = domainOfInfluence.ParentId == null;
        var allDois = await _repo.Query()
            .WhereContestIsInTestingPhaseOrNoContest()
            .ToListAsync();

        var existingDomainOfInfluence = await QueryExistingForUpdate()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        domainOfInfluence.CountingCircles = existingDomainOfInfluence.CountingCircles;
        domainOfInfluence.PlausibilisationConfiguration = existingDomainOfInfluence.PlausibilisationConfiguration;
        domainOfInfluence.SwissAbroadVotingRight = existingDomainOfInfluence.SwissAbroadVotingRight;

        // basis update does not provide a canton for non root domain of influences
        if (!isRootDomainOfInfluence)
        {
            domainOfInfluence.Canton = existingDomainOfInfluence.Canton;
        }

        var hasDifferentCantonAndIsRoot = isRootDomainOfInfluence
            && existingDomainOfInfluence.Canton != domainOfInfluence.Canton;

        await _repo.Update(domainOfInfluence);

        if (hasDifferentCantonAndIsRoot)
        {
            await _repo.UpdateInheritedCantons(id, domainOfInfluence.Canton);
            await _domainOfInfluenceCantonDefaultsBuilder.RebuildForRootDomainOfInfluenceCantonUpdate(domainOfInfluence, allDois);
        }

        var snapshots = await QueryExistingForUpdate()
            .Where(cc => cc.BasisDomainOfInfluenceId == id)
            .WhereContestIsInTestingPhase()
            .ToListAsync();

        foreach (var snapshot in snapshots)
        {
            var snapshotDomainOfInfluence = _mapper.Map<DomainOfInfluence>(eventData.DomainOfInfluence);
            snapshotDomainOfInfluence.SnapshotContestId = snapshot.SnapshotContestId!.Value;
            snapshotDomainOfInfluence.Id = snapshot.Id;
            snapshotDomainOfInfluence.ParentId = snapshot.ParentId;
            snapshotDomainOfInfluence.CountingCircles = snapshot.CountingCircles;
            snapshotDomainOfInfluence.PlausibilisationConfiguration = snapshot.PlausibilisationConfiguration;
            snapshotDomainOfInfluence.SwissAbroadVotingRight = snapshot.SwissAbroadVotingRight;

            if (!isRootDomainOfInfluence)
            {
                snapshotDomainOfInfluence.Canton = existingDomainOfInfluence.Canton;
            }

            await _repo.Update(snapshotDomainOfInfluence);

            if (hasDifferentCantonAndIsRoot)
            {
                await _repo.UpdateInheritedCantons(snapshotDomainOfInfluence.Id, domainOfInfluence.Canton);
                await _domainOfInfluenceCantonDefaultsBuilder.RebuildForRootDomainOfInfluenceCantonUpdate(snapshotDomainOfInfluence, allDois);
            }
        }

        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(DomainOfInfluenceContactPersonUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var existingDomainOfInfluence = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData.ContactPerson, existingDomainOfInfluence.ContactPerson);
        await _repo.Update(existingDomainOfInfluence);

        var snapshots = await _repo.Query()
            .Where(cc => cc.BasisDomainOfInfluenceId == id)
            .WhereContestIsInTestingPhase()
            .ToListAsync();
        foreach (var snapshot in snapshots)
        {
            // cant reuse the same object since ef tracks it with a key internally
            _mapper.Map(existingDomainOfInfluence.ContactPerson, snapshot.ContactPerson);
        }

        await _repo.UpdateRange(snapshots);
    }

    public async Task Process(DomainOfInfluencePlausibilisationConfigurationUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        var existingDomainOfInfluence = await QueryExistingForUpdate()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        await MapUpdatedPlausibilisationConfiguration(
            existingDomainOfInfluence,
            eventData.PlausibilisationConfiguration);

        await _repo.Update(existingDomainOfInfluence);

        var snapshots = await QueryExistingForUpdate()
            .Where(cc => cc.BasisDomainOfInfluenceId == id)
            .WhereContestIsInTestingPhase()
            .ToListAsync();

        foreach (var snapshot in snapshots)
        {
            await MapUpdatedPlausibilisationConfiguration(
                snapshot,
                eventData.PlausibilisationConfiguration,
                snapshot.SnapshotContestId!.Value);
        }

        await _repo.UpdateRange(snapshots);
    }

    public async Task Process(DomainOfInfluenceCountingCircleEntriesUpdated eventData)
    {
        var domainOfInfluenceId = GuidParser.Parse(eventData.DomainOfInfluenceCountingCircleEntries.Id);

        var countingCircleIds = eventData.DomainOfInfluenceCountingCircleEntries.CountingCircleIds
            .Select(GuidParser.Parse)
            .ToList();

        await UpdateCountingCircles(domainOfInfluenceId, countingCircleIds);

        var snapshots = await _repo.Query()
            .Where(cc => cc.BasisDomainOfInfluenceId == domainOfInfluenceId)
            .WhereContestIsInTestingPhase()
            .ToListAsync();
        foreach (var snapshot in snapshots)
        {
            var mappedCountingCircleIds = countingCircleIds.ConvertAll(cId => AusmittlungUuidV5.BuildCountingCircleSnapshot(snapshot.SnapshotContestId!.Value, cId));
            await UpdateCountingCircles(snapshot.Id, mappedCountingCircleIds);
        }

        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(DomainOfInfluenceDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.DomainOfInfluenceId);
        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await RemoveAssignedAndInheritedCountingCircles(id);

        await _repo.DeleteByKey(id);

        var snapshotIdsToDelete = await _repo.Query()
            .Where(doi => doi.BasisDomainOfInfluenceId == id)
            .WhereContestIsInTestingPhase()
            .Select(doi => doi.Id)
            .ToListAsync();

        foreach (var idToDelete in snapshotIdsToDelete)
        {
            await RemoveAssignedAndInheritedCountingCircles(idToDelete);
        }

        await _repo.DeleteRangeByKey(snapshotIdsToDelete);
        await _permissionBuilder.RebuildPermissionTree();
    }

    public async Task Process(DomainOfInfluencePartyCreated eventData)
    {
        var party = _mapper.Map<DomainOfInfluenceParty>(eventData.Party);
        var parties = new List<DomainOfInfluenceParty> { party };

        var dois = await _repo.ListWithContestsInTestingPhase(party.DomainOfInfluenceId);
        parties.AddRange(dois.Select(doi =>
        {
            var snapshotContestId = doi.SnapshotContestId!.Value;
            var snapshotParty = _mapper.Map<DomainOfInfluenceParty>(eventData.Party);
            snapshotParty.DomainOfInfluenceId = doi.Id;
            snapshotParty.SnapshotForContest(snapshotContestId);
            return snapshotParty;
        }));

        await _doiPartyRepo.CreateRange(parties);
    }

    public async Task Process(DomainOfInfluencePartyUpdated eventData)
    {
        var updatedParty = _mapper.Map<DomainOfInfluenceParty>(eventData.Party);
        var existingParties = await _doiPartyRepo.GetInTestingPhaseOrNoContestParties(updatedParty.Id);

        foreach (var existingParty in existingParties)
        {
            existingParty.Translations.Update(
                updatedParty.Translations,
                x => x.Language,
                (existing, updated) =>
                {
                    existing.Name = updated.Name;
                    existing.ShortDescription = updated.ShortDescription;
                },
                _db);
        }

        await _doiPartyRepo.UpdateRange(existingParties);
    }

    public async Task Process(DomainOfInfluencePartyDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        await _doiPartyRepo.DeleteByKey(id);

        var existingParties = await _doiPartyRepo.GetInTestingPhaseOrNoContestParties(id);

        foreach (var existingParty in existingParties)
        {
            existingParty.Deleted = true;
        }

        await _doiPartyRepo.UpdateRange(existingParties);
    }

    private async Task UpdateCountingCircles(Guid domainOfInfluenceId, IReadOnlyCollection<Guid> countingCircleIds)
    {
        var existing = await _repo.Query()
           .AsSplitQuery()
           .Include(x => x.CountingCircles)
           .FirstOrDefaultAsync(x => x.Id == domainOfInfluenceId)
            ?? throw new EntityNotFoundException(domainOfInfluenceId);

        var nonInheritedCountingCircleIds = existing.CountingCircles
            .Where(x => !x.Inherited)
            .Select(x => x.CountingCircleId)
            .ToList();

        var countingCircleIdsToRemove = nonInheritedCountingCircleIds.Except(countingCircleIds).ToList();
        var countingCircleIdsToAdd = countingCircleIds.Except(nonInheritedCountingCircleIds).ToList();

        var hierarchicalGreaterOrSelfDoiIds = await _doiCcInheritanceBuilder.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(domainOfInfluenceId);
        await _doiCcInheritanceBuilder.BuildInheritanceForCountingCircles(
            domainOfInfluenceId,
            hierarchicalGreaterOrSelfDoiIds,
            countingCircleIdsToAdd,
            countingCircleIdsToRemove);

        await UpdateDomainOfInfluenceCountingCircleDependentEntities(hierarchicalGreaterOrSelfDoiIds);
    }

    private async Task RemoveAssignedAndInheritedCountingCircles(Guid domainOfInfluenceId)
    {
        var assignedAndInheritedCountingCircleIds = await _repo.Query()
            .Where(x => x.Id == domainOfInfluenceId)
            .SelectMany(x => x.CountingCircles)
            .Select(x => x.CountingCircleId)
            .ToListAsync();

        var hierarchicalGreaterOrSelfDoiIds = await _doiCcInheritanceBuilder.GetHierarchicalGreaterOrSelfDomainOfInfluenceIds(domainOfInfluenceId);

        await _doiCcInheritanceBuilder.BuildInheritanceForCountingCircles(
            domainOfInfluenceId,
            hierarchicalGreaterOrSelfDoiIds,
            new(),
            assignedAndInheritedCountingCircleIds);

        await UpdateDomainOfInfluenceCountingCircleDependentEntities(hierarchicalGreaterOrSelfDoiIds);
    }

    private async Task MapUpdatedPlausibilisationConfiguration(
        DomainOfInfluence doi,
        PlausibilisationConfigurationEventData plausiConfig,
        Guid? snapshotContestId = null)
    {
        var existingPlausiConfig = doi.PlausibilisationConfiguration;

        var updatedPlausiConfig = _mapper.Map<PlausibilisationConfiguration>(plausiConfig);
        if (existingPlausiConfig == null)
        {
            doi.PlausibilisationConfiguration = updatedPlausiConfig;
        }
        else
        {
            MapUpdatedPlausibilisationConfigurationToExisting(updatedPlausiConfig, existingPlausiConfig);
        }

        var ccEntries = plausiConfig.ComparisonCountOfVotersCountingCircleEntries;
        if (ccEntries == null || ccEntries.Count == 0)
        {
            return;
        }

        var basisCcIds = ccEntries.Select(x => Guid.Parse(x.CountingCircleId)).ToList();
        var basisCcIdByCcId = snapshotContestId == null
            ? basisCcIds.ToDictionary(x => x, x => x)
            : await _countingCircleRepo.Query()
                .Where(cc => basisCcIds.Contains(cc.BasisCountingCircleId) && cc.SnapshotContestId == snapshotContestId.Value)
                .ToDictionaryAsync(cc => cc.Id, cc => cc.BasisCountingCircleId);

        var ccEntryByCcId = ccEntries.ToDictionary(x => Guid.Parse(x.CountingCircleId), x => x);

        foreach (var doiCc in doi.CountingCircles)
        {
            var hasCcEntry = basisCcIdByCcId.TryGetValue(doiCc.CountingCircleId, out var basisCcId);
            doiCc.ComparisonCountOfVotersCategory = hasCcEntry
                ? (ComparisonCountOfVotersCategory)ccEntryByCcId[basisCcId].Category
                : ComparisonCountOfVotersCategory.Unspecified;
        }
    }

    private void MapUpdatedPlausibilisationConfigurationToExisting(PlausibilisationConfiguration updatedConfig, PlausibilisationConfiguration existingConfig)
    {
        existingConfig.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent = updatedConfig.ComparisonValidVotingCardsWithAccountedBallotsThresholdPercent;

        existingConfig.ComparisonVoterParticipationConfigurations.Update(
            updatedConfig.ComparisonVoterParticipationConfigurations,
            x => (x.MainLevel, x.ComparisonLevel),
            (existing, updated) => existing.ThresholdPercent = updated.ThresholdPercent,
            _db);

        existingConfig.ComparisonCountOfVotersConfigurations.MatchAndExec(
            updatedConfig.ComparisonCountOfVotersConfigurations,
            x => x.Category,
            (existing, updated) => existing.ThresholdPercent = updated.ThresholdPercent);

        existingConfig.ComparisonVotingChannelConfigurations.MatchAndExec(
            updatedConfig.ComparisonVotingChannelConfigurations,
            x => x.VotingChannel,
            (existing, updated) => existing.ThresholdPercent = updated.ThresholdPercent);
    }

    private IQueryable<DomainOfInfluence> QueryExistingForUpdate()
    {
        return _repo.Query()
            .AsSplitQuery()
            .Include(x => x.PlausibilisationConfiguration!)
                .ThenInclude(x => x.ComparisonVoterParticipationConfigurations)
            .Include(x => x.PlausibilisationConfiguration!)
                .ThenInclude(x => x.ComparisonCountOfVotersConfigurations)
            .Include(x => x.PlausibilisationConfiguration!)
                .ThenInclude(x => x.ComparisonVotingChannelConfigurations)
            .Include(x => x.CountingCircles);
    }

    private async Task UpdateDomainOfInfluenceCountingCircleDependentEntities(List<Guid> doiIds)
    {
        // When a doi cc is changed, it could also affect parent dois (inheritance), so results on parents must also be initialized.
        await _ccResultsInitializer.InitializeResults(doiIds);
        await _contestCountingCircleDetailsBuilder.SyncForDomainOfInfluences(doiIds);
    }
}
