// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class CantonSettingsProcessor :
    IEventProcessor<CantonSettingsCreated>,
    IEventProcessor<CantonSettingsUpdated>
{
    private readonly CantonSettingsRepo _repo;
    private readonly IMapper _mapper;
    private readonly DomainOfInfluenceCantonDefaultsBuilder _cantonDefaultsBuilder;
    private readonly DataContext _dbContext;

    public CantonSettingsProcessor(
        CantonSettingsRepo repo,
        IMapper mapper,
        DomainOfInfluenceCantonDefaultsBuilder cantonDefaultsBuilder,
        DataContext dbContext)
    {
        _repo = repo;
        _mapper = mapper;
        _cantonDefaultsBuilder = cantonDefaultsBuilder;
        _dbContext = dbContext;
    }

    public async Task Process(CantonSettingsCreated eventData)
    {
        var model = _mapper.Map<CantonSettings>(eventData.CantonSettings);
        await _repo.Create(model);
        await _cantonDefaultsBuilder.RebuildForCanton(model);
    }

    public async Task Process(CantonSettingsUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.CantonSettings.Id);
        var existing = await _repo.Query()
                .AsTracking()
                .Include(x => x.EnabledVotingCardChannels)
                .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(CantonSettings), id);
        _mapper.Map(eventData.CantonSettings, existing);
        await _dbContext.SaveChangesAsync();
        await _cantonDefaultsBuilder.RebuildForCanton(existing);
    }
}
