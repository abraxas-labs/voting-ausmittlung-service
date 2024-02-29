// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Core.Mapping.WriterMappings;

public class VoteResultProfile : Profile
{
    public VoteResultProfile()
    {
        CreateMap<VoteBallotQuestionResult, VoteBallotQuestionResultsEventData>().ReverseMap();
        CreateMap<VoteResultBallotQuestionAnswer, VoteResultBallotUpdatedQuestionAnswerEventData>().ReverseMap();
        CreateMap<VoteTieBreakQuestionResult, VoteTieBreakQuestionResultsEventData>().ReverseMap();
        CreateMap<VoteResultBallotTieBreakQuestionAnswer, VoteResultBallotUpdatedTieBreakQuestionAnswerEventData>().ReverseMap();
        CreateMap<VoteResultEntryParams, VoteResultEntryParamsEventData>().ReverseMap();
        CreateMap<VoteBallotResults, VoteBallotResultsCountOfVotersEventData>().ReverseMap();
        CreateMap<VoteBallotResults, VoteBallotResultsEventData>().ReverseMap();
        CreateMap<VoteBallotResultsCountOfVoters, VoteBallotResultsCountOfVotersEventData>().ReverseMap();
    }
}
