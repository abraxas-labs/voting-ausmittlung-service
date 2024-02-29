// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionUnionProcessor :
    IEventProcessor<ProportionalElectionUnionCreated>,
    IEventProcessor<ProportionalElectionUnionUpdated>,
    IEventProcessor<ProportionalElectionUnionDeleted>,
    IEventProcessor<ProportionalElectionUnionToNewContestMoved>,
    IEventProcessor<ProportionalElectionUnionEntriesUpdated>
{
    private readonly IDbRepository<DataContext, ProportionalElectionUnion> _repo;
    private readonly ProportionalElectionUnionEntryRepo _entriesRepo;
    private readonly IMapper _mapper;
    private readonly ProportionalElectionUnionListBuilder _unionListBuilder;

    public ProportionalElectionUnionProcessor(
        IDbRepository<DataContext, ProportionalElectionUnion> repo,
        ProportionalElectionUnionEntryRepo entriesRepo,
        IMapper mapper,
        ProportionalElectionUnionListBuilder unionListBuilder)
    {
        _repo = repo;
        _entriesRepo = entriesRepo;
        _mapper = mapper;
        _unionListBuilder = unionListBuilder;
    }

    public async Task Process(ProportionalElectionUnionCreated eventData)
    {
        var model = _mapper.Map<ProportionalElectionUnion>(eventData.ProportionalElectionUnion);
        await _repo.Create(model);
    }

    public async Task Process(ProportionalElectionUnionUpdated eventData)
    {
        var model = _mapper.Map<ProportionalElectionUnion>(eventData.ProportionalElectionUnion);

        if (!await _repo.ExistsByKey(model.Id))
        {
            throw new EntityNotFoundException(model.Id);
        }

        await _repo.Update(model);
    }

    public async Task Process(ProportionalElectionUnionEntriesUpdated eventData)
    {
        var proportionalElectionUnionId = GuidParser.Parse(eventData.ProportionalElectionUnionEntries.ProportionalElectionUnionId);

        if (!await _repo.ExistsByKey(proportionalElectionUnionId))
        {
            throw new EntityNotFoundException(proportionalElectionUnionId);
        }

        var models = eventData.ProportionalElectionUnionEntries.ProportionalElectionIds.Select(electionId =>
            new ProportionalElectionUnionEntry
            {
                ProportionalElectionUnionId = proportionalElectionUnionId,
                ProportionalElectionId = GuidParser.Parse(electionId),
            }).ToList();

        await _entriesRepo.Replace(proportionalElectionUnionId, models);
        await _unionListBuilder.RebuildLists(
            proportionalElectionUnionId,
            models.Select(e => e.ProportionalElectionId).ToList());
    }

    public async Task Process(ProportionalElectionUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionUnionId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);
    }

    public async Task Process(ProportionalElectionUnionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.ProportionalElectionUnionId);

        var existingModel = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);
        await _repo.Update(existingModel);
    }
}
