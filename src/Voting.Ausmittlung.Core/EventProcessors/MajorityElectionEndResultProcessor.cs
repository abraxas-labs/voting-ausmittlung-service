// (c) Copyright 2022 by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class MajorityElectionEndResultProcessor
    : IEventProcessor<MajorityElectionEndResultLotDecisionsUpdated>,
        IEventProcessor<MajorityElectionEndResultFinalized>,
        IEventProcessor<MajorityElectionEndResultFinalizationReverted>
{
    private readonly MajorityElectionEndResultBuilder _endResultBuilder;
    private readonly MajorityElectionEndResultRepo _endResultRepo;
    private readonly IMapper _mapper;

    public MajorityElectionEndResultProcessor(
        MajorityElectionEndResultBuilder endResultBuilder,
        MajorityElectionEndResultRepo endResultRepo,
        IMapper mapper)
    {
        _endResultBuilder = endResultBuilder;
        _endResultRepo = endResultRepo;
        _mapper = mapper;
    }

    public Task Process(MajorityElectionEndResultFinalized eventData)
        => SetFinalized(eventData.MajorityElectionId, true);

    public Task Process(MajorityElectionEndResultFinalizationReverted eventData)
        => SetFinalized(eventData.MajorityElectionId, false);

    public async Task Process(MajorityElectionEndResultLotDecisionsUpdated eventData)
    {
        var majorityElectionId = Guid.Parse(eventData.MajorityElectionId);
        var lotDecisions = _mapper.Map<IEnumerable<ElectionEndResultLotDecision>>(eventData.LotDecisions);

        if (await _endResultRepo.GetByMajorityElectionId(majorityElectionId) == null)
        {
            throw new EntityNotFoundException(majorityElectionId);
        }

        await _endResultBuilder.RecalculateForLotDecisions(majorityElectionId, lotDecisions);
    }

    private async Task SetFinalized(string politicalBusinessId, bool finalized)
    {
        var endResult = await _endResultRepo.Query()
            .IgnoreQueryFilters() // do not filter translations
            .Include(x => x.MajorityElection.Translations)
            .FirstOrDefaultAsync(x => x.MajorityElectionId == GuidParser.Parse(politicalBusinessId))
            ?? throw new EntityNotFoundException(politicalBusinessId);
        endResult.Finalized = finalized;
        await _endResultRepo.Update(endResult);
    }
}
