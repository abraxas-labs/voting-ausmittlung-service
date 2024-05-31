// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class ProportionalElectionResultProfile : Profile
{
    public ProportionalElectionResultProfile()
    {
        CreateMap<ProportionalElectionUnmodifiedListResult, ProportionalElectionUnmodifiedListResultEventData>().ReverseMap();
        CreateMap<ProportionalElectionResultBallotCandidate, ProportionalElectionResultBallotUpdatedCandidateEventData>().ReverseMap();
        CreateMap<ProportionalElectionResultEntryParams, ProportionalElectionResultEntryParamsEventData>().ReverseMap();
        CreateMap<ElectionEndResultLotDecision, ProportionalElectionEndResultLotDecisionEventData>().ReverseMap();
        CreateMap<ProportionalElectionManualCandidateEndResult, ProportionalElectionManualCandidateEndResultEventData>().ReverseMap();

        CreateMap<ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated, DoubleProportionalResultSuperApportionmentLotDecision>();
        CreateMap<DoubleProportionalResultSuperApportionmentLotDecisionColumn, ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData>()
            .ForMember(dst => dst.ListId, opts => opts.MapFrom(src => src.ListId!.Value))
            .ReverseMap();
    }
}
