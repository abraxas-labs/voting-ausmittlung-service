// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

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
        CreateMap<ElectionResultEntryParams, ProportionalElectionResultEntryParamsEventData>().ReverseMap();
        CreateMap<ElectionEndResultLotDecision, ProportionalElectionEndResultLotDecisionEventData>().ReverseMap();
    }
}
