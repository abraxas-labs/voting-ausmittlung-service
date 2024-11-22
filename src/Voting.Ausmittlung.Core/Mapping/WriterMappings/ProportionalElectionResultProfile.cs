// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using Voting.Ausmittlung.Data.Models;
using ProportionalElectionResultBallotCandidate = Voting.Ausmittlung.Core.Domain.ProportionalElectionResultBallotCandidate;
using ProportionalElectionResultEntryParams = Voting.Ausmittlung.Core.Domain.ProportionalElectionResultEntryParams;
using ProportionalElectionUnmodifiedListResult = Voting.Ausmittlung.Core.Domain.ProportionalElectionUnmodifiedListResult;

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
        CreateMap<ProportionalElectionEndResultListLotDecision, ProportionalElectionEndResultListLotDecisionEventData>().ReverseMap();
        CreateMap<ProportionalElectionEndResultListLotDecisionEntry, ProportionalElectionEndResultListLotDecisionEntryEventData>().ReverseMap();

        CreateMap<ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionUpdated, DoubleProportionalResultSuperApportionmentLotDecision>();
        CreateMap<DoubleProportionalResultSuperApportionmentLotDecisionColumn, ProportionalElectionDoubleProportionalSuperApportionmentLotDecisionColumnEventData>()
            .ForMember(dst => dst.ListId, opts => opts.MapFrom(src => src.ListId!.Value))
            .ReverseMap();
    }
}
