// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Threading.Tasks;
using Abraxas.Voting.Ausmittlung.Events.V1;
using AutoMapper;
using Voting.Ausmittlung.Core.Utils.DoubleProportional;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class ProportionalElectionDoubleProportionalResultProcessor :
    IEventProcessor<ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated>
{
    private readonly DoubleProportionalResultBuilder _dpResultBuilder;
    private readonly IMapper _mapper;

    public ProportionalElectionDoubleProportionalResultProcessor(
        DoubleProportionalResultBuilder dpResultBuilder,
        IMapper mapper)
    {
        _dpResultBuilder = dpResultBuilder;
        _mapper = mapper;
    }

    public async Task Process(ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated eventData)
    {
        var electionId = GuidParser.Parse(eventData.ProportionalElectionId);
        var lotDecision = _mapper.Map<Domain.DoubleProportionalResultSuperApportionmentLotDecision>(eventData);
        await _dpResultBuilder.SetSuperApportionmentLotDecisionForElection(electionId, lotDecision);
    }
}
