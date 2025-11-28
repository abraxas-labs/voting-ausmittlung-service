// (c) Copyright by Abraxas Informatik AG
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
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Ausmittlung.Data.Repositories;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionEndResultProcessor :
    PoliticalBusinessEndResultProcessor,
    IEventProcessor<ProportionalElectionListEndResultLotDecisionsUpdated>,
    IEventProcessor<ProportionalElectionEndResultMandateDistributionStarted>,
    IEventProcessor<ProportionalElectionEndResultMandateDistributionReverted>,
    IEventProcessor<ProportionalElectionEndResultFinalized>,
    IEventProcessor<ProportionalElectionEndResultFinalizationReverted>,
    IEventProcessor<ProportionalElectionManualListEndResultEntered>,
    IEventProcessor<ProportionalElectionListEndResultListLotDecisionsUpdated>
{
    private readonly ProportionalElectionEndResultBuilder _endResultBuilder;
    private readonly ProportionalElectionEndResultLotDecisionBuilder _endResultLotDecisionBuilder;
    private readonly ProportionalElectionEndResultRepo _endResultRepo;
    private readonly ProportionalElectionCandidateEndResultBuilder _candidateEndResultBuilder;
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;
    private readonly EventLogger _eventLogger;

    public ProportionalElectionEndResultProcessor(
        ProportionalElectionEndResultBuilder endResultBuilder,
        ProportionalElectionEndResultLotDecisionBuilder endResultLotDecisionBuilder,
        ProportionalElectionEndResultRepo endResultRepo,
        IDbRepository<DataContext, SimplePoliticalBusiness> simplePoliticalBusinessRepo,
        IMapper mapper,
        ProportionalElectionCandidateEndResultBuilder candidateEndResultBuilder,
        DataContext dataContext,
        EventLogger eventLogger)
        : base(simplePoliticalBusinessRepo)
    {
        _endResultBuilder = endResultBuilder;
        _endResultRepo = endResultRepo;
        _mapper = mapper;
        _endResultLotDecisionBuilder = endResultLotDecisionBuilder;
        _candidateEndResultBuilder = candidateEndResultBuilder;
        _dataContext = dataContext;
        _eventLogger = eventLogger;
    }

    public async Task Process(ProportionalElectionEndResultMandateDistributionStarted eventData)
    {
        var electionId = GuidParser.Parse(eventData.ProportionalElectionId);
        await _endResultBuilder.DistributeNumberOfMandates(electionId);
        _eventLogger.LogEndResultEvent(eventData, Guid.Parse(eventData.ProportionalElectionEndResultId), electionId);
    }

    public async Task Process(ProportionalElectionEndResultMandateDistributionReverted eventData)
    {
        var electionId = GuidParser.Parse(eventData.ProportionalElectionId);
        await _endResultBuilder.ResetDistributedNumberOfMandatesForElection(electionId);
        _eventLogger.LogEndResultEvent(eventData, Guid.Parse(eventData.ProportionalElectionEndResultId), electionId);
    }

    public Task Process(ProportionalElectionEndResultFinalized eventData)
        => SetFinalized(GuidParser.Parse(eventData.ProportionalElectionId), true);

    public Task Process(ProportionalElectionEndResultFinalizationReverted eventData)
        => SetFinalized(GuidParser.Parse(eventData.ProportionalElectionId), false);

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

    public async Task Process(ProportionalElectionListEndResultListLotDecisionsUpdated eventData)
    {
        var electionId = Guid.Parse(eventData.ProportionalElectionId);
        var endResult = await _endResultRepo.GetByProportionalElectionIdAsTracked(electionId);
        if (endResult == null)
        {
            throw new EntityNotFoundException(electionId);
        }

        endResult.ListLotDecisions = _mapper.Map<List<ProportionalElectionEndResultListLotDecision>>(eventData.ListLotDecisions);
        await _dataContext.SaveChangesAsync();
    }

    protected override async Task SetFinalized(Guid politicalBusinessId, bool finalized)
    {
        var endResult = await _endResultRepo.Query()
                .IgnoreQueryFilters()// do not filter translations
                .Include(x => x.ProportionalElection.Translations)
                .FirstOrDefaultAsync(x => x.ProportionalElectionId == politicalBusinessId)
            ?? throw new EntityNotFoundException(politicalBusinessId);
        endResult.Finalized = finalized;
        await _endResultRepo.Update(endResult);

        await base.SetFinalized(politicalBusinessId, finalized);
    }
}
