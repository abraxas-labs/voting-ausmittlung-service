// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Voting.Ausmittlung.Core.Exceptions;
using Voting.Ausmittlung.Core.Utils;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Ausmittlung.Data;
using Voting.Ausmittlung.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Database.Repositories;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionUnionDoubleProportionalResultProcessor :
    IEventProcessor<ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated>,
    IEventProcessor<ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated>
{
    private readonly DoubleProportionalResultBuilder _dpResultBuilder;
    private readonly IMapper _mapper;
    private readonly EventLogger _eventLogger;
    private readonly IDbRepository<DataContext, ProportionalElectionUnionEndResult> _unionEndResultRepo;

    public ProportionalElectionUnionDoubleProportionalResultProcessor(
        DoubleProportionalResultBuilder dpResultBuilder,
        IMapper mapper,
        EventLogger eventLogger,
        IDbRepository<DataContext, ProportionalElectionUnionEndResult> unionEndResultRepo)
    {
        _dpResultBuilder = dpResultBuilder;
        _mapper = mapper;
        _eventLogger = eventLogger;
        _unionEndResultRepo = unionEndResultRepo;
    }

    public async Task Process(ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated eventData)
    {
        var unionId = GuidParser.Parse(eventData.ProportionalElectionUnionId);
        var endResultId = await GetEndResultId(unionId);
        var lotDecision = _mapper.Map<Domain.DoubleProportionalResultSuperApportionmentLotDecision>(eventData);
        await _dpResultBuilder.SetSuperApportionmentLotDecisionForUnion(unionId, lotDecision);
        _eventLogger.LogUnionEndResultEvent(eventData, endResultId, unionId);
    }

    public async Task Process(ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated eventData)
    {
        var unionId = GuidParser.Parse(eventData.ProportionalElectionUnionId);
        var endResultId = await GetEndResultId(unionId);
        var lotDecision = _mapper.Map<Domain.DoubleProportionalResultSubApportionmentLotDecision>(eventData);
        await _dpResultBuilder.SetSubApportionmentLotDecisionForUnion(unionId, lotDecision);
        _eventLogger.LogUnionEndResultEvent(eventData, endResultId, unionId);
    }

    private async Task<Guid> GetEndResultId(Guid unionId)
    {
        return await _unionEndResultRepo.Query()
            .Where(x => x.ProportionalElectionUnionId == unionId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync()
            ?? throw new EntityNotFoundException(unionId);
    }
}
