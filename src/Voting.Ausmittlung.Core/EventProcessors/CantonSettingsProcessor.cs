// (c) Copyright 2024 by Abraxas Informatik AG
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
    private readonly DomainOfInfluenceCantonDefaultsBuilder _doiCantonDefaultsBuilder;
    private readonly ContestCantonDefaultsBuilder _contestCantonDefaultsBuilder;
    private readonly DataContext _dbContext;

    public CantonSettingsProcessor(
        CantonSettingsRepo repo,
        IMapper mapper,
        DomainOfInfluenceCantonDefaultsBuilder doiCantonDefaultsBuilder,
        ContestCantonDefaultsBuilder contestCantonDefaultsBuilder,
        DataContext dbContext)
    {
        _repo = repo;
        _mapper = mapper;
        _doiCantonDefaultsBuilder = doiCantonDefaultsBuilder;
        _contestCantonDefaultsBuilder = contestCantonDefaultsBuilder;
        _dbContext = dbContext;
    }

    public async Task Process(CantonSettingsCreated eventData)
    {
        var model = _mapper.Map<CantonSettings>(eventData.CantonSettings);
        await _repo.Create(model);
        Migrate(model);
        await _doiCantonDefaultsBuilder.RebuildForCanton(model);
        await _contestCantonDefaultsBuilder.RebuildForCanton(model);
    }

    public async Task Process(CantonSettingsUpdated eventData)
    {
        var id = GuidParser.Parse(eventData.CantonSettings.Id);
        var existing = await _repo.Query()
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.EnabledVotingCardChannels)
                .Include(x => x.CountingCircleResultStateDescriptions)
                .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new EntityNotFoundException(nameof(CantonSettings), id);
        _mapper.Map(eventData.CantonSettings, existing);
        Migrate(existing);
        await _dbContext.SaveChangesAsync();
        await _doiCantonDefaultsBuilder.RebuildForCanton(existing);
        await _contestCantonDefaultsBuilder.RebuildForCanton(existing);
    }

    private void Migrate(CantonSettings model)
    {
        // Set default sort type value since the old eventData (before introducing the sort type) can contain the unspecified value.
        if (model.ProtocolCountingCircleSortType == ProtocolCountingCircleSortType.Unspecified)
        {
            model.ProtocolCountingCircleSortType = ProtocolCountingCircleSortType.SortNumber;
        }

        // Set default sort type value since the old eventData (before introducing the sort type) can contain the unspecified value.
        if (model.ProtocolDomainOfInfluenceSortType == ProtocolDomainOfInfluenceSortType.Unspecified)
        {
            model.ProtocolDomainOfInfluenceSortType = ProtocolDomainOfInfluenceSortType.SortNumber;
        }
    }
}
