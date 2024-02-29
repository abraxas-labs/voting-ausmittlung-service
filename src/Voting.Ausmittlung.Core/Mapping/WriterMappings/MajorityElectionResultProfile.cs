// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class MajorityElectionResultProfile : Profile
{
    public MajorityElectionResultProfile()
    {
        CreateMap<MajorityElectionBallotGroupResult, MajorityElectionBallotGroupResultEventData>().ReverseMap();
        CreateMap<SecondaryMajorityElectionResultBallot, SecondaryMajorityElectionResultBallotEventData>().ReverseMap();
        CreateMap<MajorityElectionCandidateResult, MajorityElectionCandidateResultCountEventData>().ReverseMap();
        CreateMap<SecondaryMajorityElectionCandidateResults, SecondaryMajorityElectionCandidateResultsEventData>().ReverseMap();
        CreateMap<MajorityElectionResultEntryParams, MajorityElectionResultEntryParamsEventData>().ReverseMap();
        CreateMap<ElectionEndResultLotDecision, MajorityElectionEndResultLotDecisionEventData>().ReverseMap();
    }
}
