// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Ausmittlung.Data.Utils;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionProcessor :
    IEventProcessor<ProportionalElectionCreated>,
    IEventProcessor<ProportionalElectionUpdated>,
    IEventProcessor<ProportionalElectionAfterTestingPhaseUpdated>,
    IEventProcessor<ProportionalElectionActiveStateUpdated>,
    IEventProcessor<ProportionalElectionDeleted>,
    IEventProcessor<ProportionalElectionToNewContestMoved>,
    IEventProcessor<ProportionalElectionListCreated>,
    IEventProcessor<ProportionalElectionListUpdated>,
    IEventProcessor<ProportionalElectionListAfterTestingPhaseUpdated>,
    IEventProcessor<ProportionalElectionListsReordered>,
    IEventProcessor<ProportionalElectionListDeleted>,
    IEventProcessor<ProportionalElectionListUnionCreated>,
    IEventProcessor<ProportionalElectionListUnionUpdated>,
    IEventProcessor<ProportionalElectionListUnionDeleted>,
    IEventProcessor<ProportionalElectionListUnionsReordered>,
    IEventProcessor<ProportionalElectionListUnionEntriesUpdated>,
    IEventProcessor<ProportionalElectionListUnionMainListUpdated>,
    IEventProcessor<ProportionalElectionCandidateCreated>,
    IEventProcessor<ProportionalElectionCandidateUpdated>,
    IEventProcessor<ProportionalElectionCandidateAfterTestingPhaseUpdated>,
    IEventProcessor<ProportionalElectionCandidatesReordered>,
    IEventProcessor<ProportionalElectionCandidateDeleted>,
    IEventProcessor<ProportionalElectionMandateAlgorithmUpdated>
{
    private readonly ILogger<ProportionalElectionProcessor> _logger;
    private readonly IMapper _mapper;
    private readonly ProportionalElectionRepo _repo;
    private readonly ProportionalElectionTranslationRepo _translationRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionList> _listRepo;
    private readonly ProportionalElectionListTranslationRepo _listTranslationRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionListUnion> _listUnionRepo;
    private readonly ProportionalElectionListUnionTranslationRepo _listUnionTranslationRepo;
    private readonly IDbRepository<DataContext, ProportionalElectionCandidate> _candidateRepo;
    private readonly ProportionalElectionCandidateTranslationRepo _candidateTranslationRepo;
    private readonly ProportionalElectionListUnionEntryRepo _proportionalElectionListUnionEntryRepo;
    private readonly ProportionalElectionResultBuilder _resultBuilder;
    private readonly ProportionalElectionCandidateResultBuilder _candidateResultBuilder;
    private readonly ProportionalElectionUnionListBuilder _unionListBuilder;
    private readonly ProportionalElectionEndResultInitializer _endResultInitializer;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly SimplePoliticalBusinessBuilder<ProportionalElection> _simplePoliticalBusinessBuilder;
    private readonly PoliticalBusinessToNewContestMover<ProportionalElection, ProportionalElectionRepo> _politicalBusinessToNewContestMover;
    private readonly ProportionalElectionCandidateRepo _proportionalElectionCandidateRepo;
    private readonly ProportionalElectionEndResultBuilder _endResultBuilder;

    public ProportionalElectionProcessor(
        ILogger<ProportionalElectionProcessor> logger,
        IMapper mapper,
        ProportionalElectionRepo repo,
        ProportionalElectionTranslationRepo translationRepo,
        IDbRepository<DataContext, ProportionalElectionList> listRepo,
        ProportionalElectionListTranslationRepo listTranslationRepo,
        IDbRepository<DataContext, ProportionalElectionCandidate> candidateRepo,
        ProportionalElectionCandidateTranslationRepo candidateTranslationRepo,
        ProportionalElectionListUnionEntryRepo proportionalElectionListUnionEntryRepo,
        IDbRepository<DataContext, ProportionalElectionListUnion> listUnionRepo,
        ProportionalElectionListUnionTranslationRepo listUnionTranslationRepo,
        ProportionalElectionResultBuilder resultBuilder,
        ProportionalElectionCandidateResultBuilder candidateResultBuilder,
        ProportionalElectionEndResultInitializer endResultInitializer,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        ProportionalElectionUnionListBuilder unionListBuilder,
        SimplePoliticalBusinessBuilder<ProportionalElection> simplePoliticalBusinessBuilder,
        PoliticalBusinessToNewContestMover<ProportionalElection, ProportionalElectionRepo> politicalBusinessToNewContestMover,
        ProportionalElectionCandidateRepo proportionalElectionCandidateRepo,
        ProportionalElectionEndResultBuilder endResultBuilder)
    {
        _logger = logger;
        _mapper = mapper;
        _repo = repo;
        _translationRepo = translationRepo;
        _listRepo = listRepo;
        _listTranslationRepo = listTranslationRepo;
        _candidateRepo = candidateRepo;
        _candidateTranslationRepo = candidateTranslationRepo;
        _proportionalElectionListUnionEntryRepo = proportionalElectionListUnionEntryRepo;
        _listUnionRepo = listUnionRepo;
        _listUnionTranslationRepo = listUnionTranslationRepo;
        _resultBuilder = resultBuilder;
        _candidateResultBuilder = candidateResultBuilder;
        _endResultInitializer = endResultInitializer;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _unionListBuilder = unionListBuilder;
        _simplePoliticalBusinessBuilder = simplePoliticalBusinessBuilder;
        _politicalBusinessToNewContestMover = politicalBusinessToNewContestMover;
        _proportionalElectionCandidateRepo = proportionalElectionCandidateRepo;
        _endResultBuilder = endResultBuilder;
    }

    public async Task Process(ProportionalElectionCreated eventData)
    {
        var proportionalElection = _mapper.Map<ProportionalElection>(eventData.ProportionalElection);
        proportionalElection.DomainOfInfluenceId =
            AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(proportionalElection.ContestId, proportionalElection.DomainOfInfluenceId);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (proportionalElection.ReviewProcedure == ProportionalElectionReviewProcedure.Unspecified)
        {
            proportionalElection.ReviewProcedure = ProportionalElectionReviewProcedure.Electronically;
        }

        await _repo.Create(proportionalElection);

        await _resultBuilder.RebuildForElection(proportionalElection.Id, proportionalElection.DomainOfInfluenceId, false);
        await _endResultInitializer.RebuildForElection(proportionalElection.Id, false);
        await _simplePoliticalBusinessBuilder.Create(proportionalElection);
    }

    public async Task Process(ProportionalElectionUpdated eventData)
    {
        var proportionalElection = _mapper.Map<ProportionalElection>(eventData.ProportionalElection);
        proportionalElection.DomainOfInfluenceId = AusmittlungUuidV5.BuildDomainOfInfluenceSnapshot(proportionalElection.ContestId, proportionalElection.DomainOfInfluenceId);

        // Set default review procedure value since the old eventData (before introducing the review procedure) can contain the unspecified value.
        if (proportionalElection.ReviewProcedure == ProportionalElectionReviewProcedure.Unspecified)
        {
            proportionalElection.ReviewProcedure = ProportionalElectionReviewProcedure.Electronically;
        }

        var existingModel = await _repo.GetByKey(proportionalElection.Id)
            ?? throw new EntityNotFoundException(proportionalElection.Id);

        await _translationRepo.DeleteRelatedTranslations(proportionalElection.Id);
        await _repo.Update(proportionalElection);

        if (proportionalElection.DomainOfInfluenceId != existingModel.DomainOfInfluenceId)
        {
            await _resultBuilder.RebuildForElection(proportionalElection.Id, proportionalElection.DomainOfInfluenceId, false);
            await _endResultInitializer.RebuildForElection(proportionalElection.Id, false);
        }

        await _simplePoliticalBusinessBuilder.Update(proportionalElection, false);
    }

    public async Task Process(ProportionalElectionAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var proportionalElection = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, proportionalElection);
        await _translationRepo.DeleteRelatedTranslations(proportionalElection.Id);
        await _repo.Update(proportionalElection);

        await _simplePoliticalBusinessBuilder.Update(proportionalElection, true);

        _logger.LogInformation("Proportional election {ProportionalElectionId} updated after testing phase ended", id);
    }

    public async Task Process(ProportionalElectionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);
        await _unionListBuilder.RemoveListsWithNoEntries();

        await _simplePoliticalBusinessBuilder.Delete(id);
    }

    public async Task Process(ProportionalElectionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionId);
        var newContestId = GuidParser.Parse(eventData.NewContestId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _proportionalElectionCandidateRepo.UpdateReferencesForNewContest(id, newContestId);
        await _politicalBusinessToNewContestMover.Move(id, newContestId);
        await _simplePoliticalBusinessBuilder.MoveToNewContest(id, newContestId);
    }

    public async Task Process(ProportionalElectionActiveStateUpdated eventData)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var existingModel = await _repo.GetByKey(proportionalElectionId)
            ?? throw new EntityNotFoundException(proportionalElectionId);

        existingModel.Active = eventData.Active;
        await _repo.Update(existingModel);

        await _simplePoliticalBusinessBuilder.Update(existingModel, false);
    }

    public async Task Process(ProportionalElectionListCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionList>(eventData.ProportionalElectionList);
        await _listRepo.Create(model);
        await _resultBuilder.InitializeForList(model.ProportionalElectionId, model.Id);
        await _endResultInitializer.InitializeForList(model.Id);

        await _unionListBuilder.RebuildForProportionalElection(model.ProportionalElectionId);
    }

    public async Task Process(ProportionalElectionListUpdated eventData)
    {
        var list = _mapper.Map<ProportionalElectionList>(eventData.ProportionalElectionList);

        await _listTranslationRepo.DeleteRelatedTranslations(list.Id);
        await _listRepo.Update(list);
        await _unionListBuilder.RebuildForProportionalElection(list.ProportionalElectionId);
    }

    public async Task Process(ProportionalElectionListAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var list = await _listRepo.Query()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(id);

        _mapper.Map(eventData, list);
        await _listTranslationRepo.DeleteRelatedTranslations(list.Id);
        await _listRepo.Update(list);
        await _unionListBuilder.RebuildForProportionalElection(list.Id);

        _logger.LogInformation("Proportional election list {ProportionalElectionListId} updated after testing phase ended", id);
    }

    public async Task Process(ProportionalElectionListsReordered eventData)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var lists = await _listRepo.Query()
            .Where(c => c.ProportionalElectionId == proportionalElectionId)
            .ToListAsync();

        var grouped = eventData.ListOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Single().Position);

        foreach (var list in lists)
        {
            list.Position = grouped[list.Id];
        }

        await _listRepo.UpdateRange(lists);
    }

    public async Task Process(ProportionalElectionListDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionListId);

        var existingList = await _listRepo.GetByKey(id);
        if (existingList == null)
        {
            throw new EntityNotFoundException(id);
        }

        await _listRepo.DeleteByKey(id);

        var listsToUpdate = await _listRepo.Query()
            .Where(l => l.ProportionalElectionId == existingList.ProportionalElectionId && l.Position > existingList.Position)
            .ToListAsync();
        foreach (var list in listsToUpdate)
        {
            list.Position--;
        }

        await _listRepo.UpdateRange(listsToUpdate);
        await _unionListBuilder.RemoveListsWithNoEntries();
    }

    public async Task Process(ProportionalElectionListUnionCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionListUnion>(eventData.ProportionalElectionListUnion);
        await _listUnionRepo.Create(model);
    }

    public async Task Process(ProportionalElectionListUnionUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionListUnion>(eventData.ProportionalElectionListUnion);
        var existingListUnion = await _listUnionRepo.GetByKey(model.Id)
            ?? throw new EntityNotFoundException(model.Id);

        existingListUnion.Translations = model.Translations;
        await _listUnionTranslationRepo.DeleteRelatedTranslations(model.Id);
        await _listUnionRepo.Update(existingListUnion);
    }

    public async Task Process(ProportionalElectionListUnionsReordered eventData)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var proportionalElectionRootListUnionId = !string.IsNullOrEmpty(eventData.ProportionalElectionRootListUnionId)
            ? Guid.Parse(eventData.ProportionalElectionRootListUnionId)
            : (Guid?)null;

        var listUnions = await _listUnionRepo.Query()
            .Where(c => c.ProportionalElectionId == proportionalElectionId && c.ProportionalElectionRootListUnionId == proportionalElectionRootListUnionId)
            .ToListAsync();

        var grouped = eventData.ProportionalElectionListUnionOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Select(y => y.Position).OrderBy(y => y).ToList());

        foreach (var listUnion in listUnions)
        {
            listUnion.Position = grouped[listUnion.Id][0];
        }

        await _listUnionRepo.UpdateRange(listUnions);
    }

    public async Task Process(ProportionalElectionListUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionListUnionId);

        var existingListUnion = await _listUnionRepo.GetByKey(id);
        if (existingListUnion == null)
        {
            throw new EntityNotFoundException(id);
        }

        await _listUnionRepo.DeleteByKey(id);

        var listUnionsToUpdate = await _listUnionRepo.Query()
            .Where(l => l.ProportionalElectionId == existingListUnion.ProportionalElectionId && l.Position > existingListUnion.Position)
            .ToListAsync();

        foreach (var listUnion in listUnionsToUpdate)
        {
            listUnion.Position--;
        }

        await _listUnionRepo.UpdateRange(listUnionsToUpdate);
    }

    public async Task Process(ProportionalElectionListUnionEntriesUpdated eventData)
    {
        var listUnionKey = eventData.ProportionalElectionListUnionEntries.ProportionalElectionListUnionId;
        var listUnionId = GuidParser.Parse(listUnionKey);

        var listUnion = await _listUnionRepo.Query()
                            .Include(lu => lu.ProportionalElectionSubListUnions)
                            .FirstOrDefaultAsync(x => x.Id == listUnionId)
                        ?? throw new EntityNotFoundException(listUnionId);

        var newListIds = eventData.ProportionalElectionListUnionEntries.ProportionalElectionListIds.ToList();
        var entries = newListIds.Select(listId => new ProportionalElectionListUnionEntry
        {
            ProportionalElectionListId = GuidParser.Parse(listId),
            ProportionalElectionListUnionId = listUnionId,
        }).ToList();

        await _proportionalElectionListUnionEntryRepo.Replace(listUnionId, entries);

        // delete main list id in sub list union if it isn't in the new entries
        if (listUnion.IsSubListUnion
            && listUnion.ProportionalElectionMainListId.HasValue
            && !newListIds.Contains(listUnion.ProportionalElectionMainListId.Value.ToString()))
        {
            listUnion.ProportionalElectionMainListId = null;
            await _listUnionRepo.Update(listUnion);
        }

        if (!listUnion.IsSubListUnion)
        {
            await UpdateSubListUnionsByRootListUnionEntries(listUnion, entries);
        }
    }

    public async Task Process(ProportionalElectionListUnionMainListUpdated eventData)
    {
        var listUnionId = GuidParser.Parse(eventData.ProportionalElectionListUnionId);
        var listUnion = await _listUnionRepo.GetByKey(listUnionId)
            ?? throw new EntityNotFoundException(listUnionId);

        listUnion.ProportionalElectionMainListId = string.IsNullOrEmpty(eventData.ProportionalElectionMainListId)
            ? null
            : GuidParser.Parse(eventData.ProportionalElectionMainListId);
        listUnion.ProportionalElectionMainList = null;

        await _listUnionRepo.Update(listUnion);
    }

    public async Task Process(ProportionalElectionCandidateCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionCandidate>(eventData.ProportionalElectionCandidate);

        var contestId = await _listRepo.Query()
                .Where(l => l.Id == model.ProportionalElectionListId)
                .Select(l => (Guid?)l.ProportionalElection.ContestId)
                .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(nameof(ProportionalElectionList), model.ProportionalElectionListId);

        if (model.PartyId != null)
        {
            model.PartyId = AusmittlungUuidV5.BuildDomainOfInfluenceParty(
                contestId,
                model.PartyId.Value);
        }

        await _candidateRepo.Create(model);
        await _candidateResultBuilder.Initialize(model.ProportionalElectionListId, model.Id);
        await _candidateEndResultBuilder.Initialize(model.Id);
    }

    public async Task Process(ProportionalElectionCandidateUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionCandidate>(eventData.ProportionalElectionCandidate);

        var (existingCandidate, contestId) = await _candidateRepo
            .Query()
            .Where(c => c.Id == model.Id)
            .Select(c => new Tuple<ProportionalElectionCandidate, Guid>(c, c.ProportionalElectionList.ProportionalElection.ContestId))
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(model.Id);

        if (model.PartyId != null)
        {
            model.PartyId = AusmittlungUuidV5.BuildDomainOfInfluenceParty(
                contestId,
                model.PartyId.Value);
        }

        await _candidateTranslationRepo.DeleteRelatedTranslations(model.Id);
        await _candidateRepo.Update(model);

        var removedAccumulation = existingCandidate.Accumulated && !model.Accumulated;
        if (!removedAccumulation)
        {
            return;
        }

        var accumulatedPosition = existingCandidate.AccumulatedPosition;
        var candidatesToUpdate = await _candidateRepo.Query()
            .Where(c => c.ProportionalElectionListId == existingCandidate.ProportionalElectionListId
                        && (c.Position > accumulatedPosition
                            || (c.Accumulated && c.AccumulatedPosition > accumulatedPosition)))
            .ToListAsync();

        DecreaseCandidatePositions(candidatesToUpdate, accumulatedPosition);
        await _candidateRepo.UpdateRange(candidatesToUpdate);
    }

    public async Task Process(ProportionalElectionCandidateAfterTestingPhaseUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.Id);
        var candidate = await _candidateRepo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);
        _mapper.Map(eventData, candidate);

        await _candidateTranslationRepo.DeleteRelatedTranslations(candidate.Id);
        await _candidateRepo.Update(candidate);

        _logger.LogInformation("Proportional election candidate {ProportionalElectionCandidateId} updated after testing phase ended", id);
    }

    public async Task Process(ProportionalElectionCandidatesReordered eventData)
    {
        var listId = GuidParser.Parse(eventData.ProportionalElectionListId);
        var candidates = await _candidateRepo.Query()
            .Where(c => c.ProportionalElectionListId == listId)
            .ToListAsync();

        var grouped = eventData.CandidateOrders.Orders
            .GroupBy(o => o.Id)
            .ToDictionary(x => GuidParser.Parse(x.Key), x => x.Select(y => y.Position).OrderBy(y => y).ToList());

        foreach (var candidate in candidates)
        {
            candidate.Position = grouped[candidate.Id][0];
            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition = grouped[candidate.Id][1];
            }
        }

        await _candidateRepo.UpdateRange(candidates);
    }

    public async Task Process(ProportionalElectionCandidateDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionCandidateId);

        var existingCandidate = await _candidateRepo.GetByKey(id);
        if (existingCandidate == null)
        {
            throw new EntityNotFoundException(id);
        }

        await _candidateRepo.DeleteByKey(id);

        var candidatesToUpdate = await _candidateRepo.Query()
            .Where(c => c.ProportionalElectionListId == existingCandidate.ProportionalElectionListId
                && c.Position > existingCandidate.Position)
            .ToListAsync();
        DecreaseCandidatePositions(candidatesToUpdate, existingCandidate.Position);
        if (existingCandidate.Accumulated)
        {
            DecreaseCandidatePositions(candidatesToUpdate, existingCandidate.AccumulatedPosition);
        }

        await _candidateRepo.UpdateRange(candidatesToUpdate);
    }

    public async Task Process(ProportionalElectionMandateAlgorithmUpdated eventData)
    {
        var proportionalElectionId = GuidParser.Parse(eventData.ProportionalElectionId);

        await _endResultBuilder.ResetForElection(proportionalElectionId);

        var existingModel = await _repo.GetByKey(proportionalElectionId)
                            ?? throw new EntityNotFoundException(proportionalElectionId);

        existingModel.MandateAlgorithm = _mapper.Map<ProportionalElectionMandateAlgorithm>(eventData.MandateAlgorithm);
        await _repo.Update(existingModel);
    }

    private async Task UpdateSubListUnionsByRootListUnionEntries(ProportionalElectionListUnion rootListUnion, List<ProportionalElectionListUnionEntry> rootListUnionEntries)
    {
        var subListUnions = rootListUnion.ProportionalElectionSubListUnions;
        var rootEntryListIds = rootListUnionEntries.Select(e => e.ProportionalElectionListId).ToList();

        await _proportionalElectionListUnionEntryRepo.DeleteSubListUnionListEntriesByRootListIds(
            subListUnions.Select(lu => lu.Id).ToList(),
            rootEntryListIds);

        var subListUnionsWithRemovedRootListUnionEntry = subListUnions
            .Where(lu => lu.ProportionalElectionMainListId.HasValue && !rootEntryListIds.Contains(lu.ProportionalElectionMainListId.Value))
            .ToList();

        foreach (var subListUnion in subListUnionsWithRemovedRootListUnionEntry)
        {
            subListUnion.ProportionalElectionMainListId = null;
        }

        await _listUnionRepo.UpdateRange(subListUnionsWithRemovedRootListUnionEntry);
    }

    private void DecreaseCandidatePositions(IEnumerable<ProportionalElectionCandidate> candidates, int fromPosition)
    {
        foreach (var candidate in candidates.Where(c => c.Position > fromPosition))
        {
            candidate.Position--;
            if (candidate.Accumulated)
            {
                candidate.AccumulatedPosition--;
            }
        }
    }
}
