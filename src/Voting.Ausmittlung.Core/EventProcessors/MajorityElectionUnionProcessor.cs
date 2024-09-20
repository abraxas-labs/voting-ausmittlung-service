// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionUnionProcessor :
    IEventProcessor<MajorityElectionUnionCreated>,
    IEventProcessor<MajorityElectionUnionUpdated>,
    IEventProcessor<MajorityElectionUnionDeleted>,
    IEventProcessor<MajorityElectionUnionToNewContestMoved>,
    IEventProcessor<MajorityElectionUnionEntriesUpdated>
{
    private readonly IDbRepository<DataContext, MajorityElectionUnion> _repo;
    private readonly MajorityElectionUnionEntryRepo _entriesRepo;
    private readonly IMapper _mapper;

    public MajorityElectionUnionProcessor(
        IDbRepository<DataContext, MajorityElectionUnion> repo,
        MajorityElectionUnionEntryRepo entriesRepo,
        IMapper mapper)
    {
        _repo = repo;
        _entriesRepo = entriesRepo;
        _mapper = mapper;
    }

    public async Task Process(MajorityElectionUnionCreated eventData)
    {
        var model = _mapper.Map<MajorityElectionUnion>(eventData.MajorityElectionUnion);
        await _repo.Create(model);
    }

    public async Task Process(MajorityElectionUnionUpdated eventData)
    {
        var model = _mapper.Map<MajorityElectionUnion>(eventData.MajorityElectionUnion);

        if (!await _repo.ExistsByKey(model.Id))
        {
            throw new EntityNotFoundException(model.Id);
        }

        await _repo.Update(model);
    }

    public async Task Process(MajorityElectionUnionEntriesUpdated eventData)
    {
        var majorityElectionUnionId = GuidParser.Parse(eventData.MajorityElectionUnionEntries.MajorityElectionUnionId);

        if (!await _repo.ExistsByKey(majorityElectionUnionId))
        {
            throw new EntityNotFoundException(majorityElectionUnionId);
        }

        var models = eventData.MajorityElectionUnionEntries.MajorityElectionIds.Select(electionId =>
            new MajorityElectionUnionEntry
            {
                MajorityElectionUnionId = majorityElectionUnionId,
                MajorityElectionId = GuidParser.Parse(electionId),
            }).ToList();

        await _entriesRepo.Replace(majorityElectionUnionId, models);
    }

    public async Task Process(MajorityElectionUnionDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionUnionId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);
    }

    public async Task Process(MajorityElectionUnionToNewContestMoved eventData)
    {
        var id = GuidParser.Parse(eventData.MajorityElectionUnionId);

        var existingModel = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(id);

        existingModel.ContestId = GuidParser.Parse(eventData.NewContestId);
        await _repo.Update(existingModel);
    }
}
