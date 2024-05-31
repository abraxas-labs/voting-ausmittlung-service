// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionUnionDoubleProportionalResultProcessor :
    IEventProcessor<ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated>,
    IEventProcessor<ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated>
{
    private readonly DoubleProportionalResultBuilder _dpResultBuilder;
    private readonly IMapper _mapper;

    public ProportionalElectionUnionDoubleProportionalResultProcessor(
        DoubleProportionalResultBuilder dpResultBuilder,
        IMapper mapper)
    {
        _dpResultBuilder = dpResultBuilder;
        _mapper = mapper;
    }

    public async Task Process(ProportionalElectionUnionDoubleProportionalSuperApportionmentLotDecisionUpdated eventData)
    {
        var unionId = GuidParser.Parse(eventData.ProportionalElectionUnionId);
        var lotDecision = _mapper.Map<Domain.DoubleProportionalResultSuperApportionmentLotDecision>(eventData);
        await _dpResultBuilder.SetSuperApportionmentLotDecisionForUnion(unionId, lotDecision);
    }

    public async Task Process(ProportionalElectionUnionDoubleProportionalSubApportionmentLotDecisionUpdated eventData)
    {
        var unionId = GuidParser.Parse(eventData.ProportionalElectionUnionId);
        var lotDecision = _mapper.Map<Domain.DoubleProportionalResultSubApportionmentLotDecision>(eventData);
        await _dpResultBuilder.SetSubApportionmentLotDecisionForUnion(unionId, lotDecision);
    }
}
