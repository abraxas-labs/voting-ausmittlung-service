// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionEndResultProcessor :
    PoliticalBusinessEndResultProcessor,
    IEventProcessor<MajorityElectionEndResultLotDecisionsUpdated>,
    IEventProcessor<MajorityElectionEndResultSecondaryLotDecisionsUpdated>,
    IEventProcessor<MajorityElectionEndResultFinalized>,
    IEventProcessor<MajorityElectionEndResultFinalizationReverted>
{
    private readonly MajorityElectionEndResultBuilder _endResultBuilder;
    private readonly MajorityElectionEndResultRepo _endResultRepo;
    private readonly IMapper _mapper;
    private readonly EventLogger _eventLogger;

    public MajorityElectionEndResultProcessor(
        MajorityElectionEndResultBuilder endResultBuilder,
        MajorityElectionEndResultRepo endResultRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo,
        IMapper mapper,
        EventLogger eventLogger)
        : base(simplePoliticalBusinessRepo)
    {
        _endResultBuilder = endResultBuilder;
        _endResultRepo = endResultRepo;
        _mapper = mapper;
        _eventLogger = eventLogger;
    }

    public Task Process(MajorityElectionEndResultFinalized eventData)
        => SetFinalized(GuidParser.Parse(eventData.MajorityElectionId), true);

    public Task Process(MajorityElectionEndResultFinalizationReverted eventData)
        => SetFinalized(GuidParser.Parse(eventData.MajorityElectionId), false);

    public async Task Process(MajorityElectionEndResultLotDecisionsUpdated eventData)
    {
        var majorityElectionId = Guid.Parse(eventData.MajorityElectionId);
        var lotDecisions = _mapper.Map<IEnumerable<ElectionEndResultLotDecision>>(eventData.LotDecisions);

        if (await _endResultRepo.GetByMajorityElectionId(majorityElectionId) == null)
        {
            throw new EntityNotFoundException(majorityElectionId);
        }

        await _endResultBuilder.SetPrimaryLotDecisions(majorityElectionId, lotDecisions);
        _eventLogger.LogEndResultEvent(eventData, Guid.Parse(eventData.MajorityElectionEndResultId), majorityElectionId);
    }

    public async Task Process(MajorityElectionEndResultSecondaryLotDecisionsUpdated eventData)
    {
        var majorityElectionId = Guid.Parse(eventData.MajorityElectionId);
        var lotDecisions = _mapper.Map<IEnumerable<ElectionEndResultLotDecision>>(eventData.LotDecisions);

        if (await _endResultRepo.GetByMajorityElectionId(majorityElectionId) == null)
        {
            throw new EntityNotFoundException(majorityElectionId);
        }

        await _endResultBuilder.SetSecondaryLotDecisions(majorityElectionId, lotDecisions);
        _eventLogger.LogEndResultEvent(eventData, Guid.Parse(eventData.MajorityElectionEndResultId), majorityElectionId);
    }

    protected override async Task SetFinalized(Guid politicalBusinessId, bool finalized)
    {
        var endResult = await _endResultRepo.Query()
                .IgnoreQueryFilters()// do not filter translations
                .Include(x => x.MajorityElection.Translations)
                .FirstOrDefaultAsync(x => x.MajorityElectionId == politicalBusinessId)
            ?? throw new EntityNotFoundException(politicalBusinessId);
        endResult.Finalized = finalized;
        await _endResultRepo.Update(endResult);

        await base.SetFinalized(politicalBusinessId, finalized);
    }
}
