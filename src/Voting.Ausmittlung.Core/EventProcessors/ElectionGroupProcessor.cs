// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ElectionGroupProcessor :
    IEventProcessor<ElectionGroupCreated>,
    IEventProcessor<ElectionGroupUpdated>,
    IEventProcessor<ElectionGroupDeleted>
{
    private readonly IDbRepository<DataContext, ElectionGroup> _repo;
    private readonly IMapper _mapper;

    public ElectionGroupProcessor(IDbRepository<DataContext, ElectionGroup> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task Process(ElectionGroupCreated eventData)
    {
        var model = _mapper.Map<ElectionGroup>(eventData.ElectionGroup);
        await _repo.Create(model);
    }

    public async Task Process(ElectionGroupUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.ElectionGroupId);
        var model = await _repo.GetByKey(id)
            ?? throw new EntityNotFoundException(eventData.ElectionGroupId);

        model.Description = eventData.Description;
        await _repo.Update(model);
    }

    public async Task Process(ElectionGroupDeleted eventData)
    {
        var id = GuidParser.Parse(eventData.ElectionGroupId);

        if (!await _repo.ExistsByKey(id))
        {
            throw new EntityNotFoundException(id);
        }

        await _repo.DeleteByKey(id);
    }
}
