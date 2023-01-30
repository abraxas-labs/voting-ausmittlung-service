// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionEndResultProcessor :
    IEventProcessor<ProportionalElectionListEndResultLotDecisionsUpdated>,
    IEventProcessor<ProportionalElectionEndResultFinalized>,
    IEventProcessor<ProportionalElectionEndResultFinalizationReverted>,
    IEventProcessor<ProportionalElectionManualListEndResultEntered>
{
    private readonly ProportionalElectionEndResultLotDecisionBuilder _endResultLotDecisionBuilder;
    private readonly ProportionalElectionEndResultRepo _endResultRepo;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly IMapper _mapper;

    public ProportionalElectionEndResultProcessor(
        ProportionalElectionEndResultLotDecisionBuilder endResultLotDecisionBuilder,
        ProportionalElectionEndResultRepo endResultRepo,
        IMapper mapper,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder)
    {
        _endResultRepo = endResultRepo;
        _mapper = mapper;
        _endResultLotDecisionBuilder = endResultLotDecisionBuilder;
        _candidateEndResultBuilder = candidateEndResultBuilder;
    }

    public Task Process(ProportionalElectionEndResultFinalized eventData)
        => SetFinalized(eventData.ProportionalElectionId, true);

    public Task Process(ProportionalElectionEndResultFinalizationReverted eventData)
        => SetFinalized(eventData.ProportionalElectionId, false);

    public async Task Process(ProportionalElectionListEndResultLotDecisionsUpdated eventData)
    {
        var electionId = Guid.Parse(eventData.ProportionalElectionId);
        var listId = Guid.Parse(eventData.ProportionalElectionListId);
        var lotDecisions = _mapper.Map<IEnumerable<ElectionEndResultLotDecision>>(eventData.LotDecisions);

        if (await _endResultRepo.GetByProportionalElectionId(electionId) == null)
        {
            throw new EntityNotFoundException(electionId);
        }

        await _endResultLotDecisionBuilder.Recalculate(listId, lotDecisions);
    }

    public async Task Process(ProportionalElectionManualListEndResultEntered eventData)
    {
        var listId = GuidParser.Parse(eventData.ProportionalElectionListId);
        var candidateStateById = eventData.CandidateEndResults.ToDictionary(
            x => GuidParser.Parse(x.CandidateId),
            x => _mapper.Map<ProportionalElectionCandidateEndResultState>(x.State));

        await _candidateEndResultBuilder.SetCandidateEndResultsManually(listId, candidateStateById);
    }

    private async Task SetFinalized(string politicalBusinessId, bool finalized)
    {
        var endResult = await _endResultRepo.Query()
            .IgnoreQueryFilters() // do not filter translations
            .Include(x => x.ProportionalElection.Translations)
            .FirstOrDefaultAsync(x => x.ProportionalElectionId == GuidParser.Parse(politicalBusinessId))
            ?? throw new EntityNotFoundException(politicalBusinessId);
        endResult.Finalized = finalized;
        await _endResultRepo.Update(endResult);
    }
}
