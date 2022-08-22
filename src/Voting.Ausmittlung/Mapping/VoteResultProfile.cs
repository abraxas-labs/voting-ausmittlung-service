// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Events.V1;
using Abraxas.Voting.Ausmittlung.Events.V1.Data;
using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using Abraxas.Voting.Ausmittlung.Services.V1.Responses;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class VoteResultProfile : Profile
{
    public VoteResultProfile()
    {
        // read
        CreateMap<DataModels.VoteResult, ProtoModels.VoteResult>()
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(src => src.CountingCircle.BasisCountingCircleId));
        CreateMap<DataModels.BallotResult, ProtoModels.BallotResult>();
        CreateMap<DataModels.BallotQuestionResult, ProtoModels.BallotQuestionResult>();
        CreateMap<DataModels.TieBreakQuestionResult, ProtoModels.TieBreakQuestionResult>();
        CreateMap<DataModels.BallotQuestionResultSubTotal, ProtoModels.BallotQuestionResultSubTotal>();
        CreateMap<DataModels.BallotQuestionResultNullableSubTotal, ProtoModels.BallotQuestionResultNullableSubTotal>();
        CreateMap<DataModels.TieBreakQuestionResultSubTotal, ProtoModels.TieBreakQuestionResultSubTotal>();
        CreateMap<DataModels.TieBreakQuestionResultNullableSubTotal, ProtoModels.TieBreakQuestionResultNullableSubTotal>();

        CreateMap<DataModels.VoteEndResult, ProtoModels.VoteEndResult>()
            .ForMember(dst => dst.Contest, opts => opts.MapFrom(src => src.Vote.Contest))
            .ForMember(dst => dst.DomainOfInfluenceDetails, opts => opts.MapFrom(src => src.Vote.DomainOfInfluence.Details));
        CreateMap<DataModels.BallotEndResult, ProtoModels.BallotEndResult>();
        CreateMap<DataModels.BallotQuestionEndResult, ProtoModels.BallotQuestionEndResult>();
        CreateMap<DataModels.TieBreakQuestionEndResult, ProtoModels.TieBreakQuestionEndResult>();

        CreateMap<DataModels.VoteResultEntryParams, ProtoModels.VoteResultEntryParams>();

        CreateMap<DataModels.BallotResult, ProtoModels.VoteResultBundles>()
            .ForMember(dst => dst.BallotResult, opts => opts.MapFrom(src => src));
        CreateMap<DataModels.VoteResultBundle, ProtoModels.VoteResultBundle>();
        CreateMap<DataModels.VoteResultBundle, GetVoteResultBundleResponse>()
            .ForMember(dst => dst.Bundle, opts => opts.MapFrom(src => src))
            .ForMember(dst => dst.VoteResult, opts => opts.MapFrom(src => src.BallotResult.VoteResult));
        CreateMap<DataModels.VoteResultBallot, ProtoModels.VoteResultBallot>()
            .ForMember(dst => dst.QuestionAnswers, opts => opts.MapFrom(src => src.QuestionAnswers))
            .ForMember(dst => dst.TieBreakQuestionAnswers, opts => opts.MapFrom(src => src.TieBreakQuestionAnswers));

        CreateMap<DataModels.VoteResultBallotQuestionAnswer, ProtoModels.VoteResultBallotQuestionAnswer>();
        CreateMap<DataModels.VoteResultBallotTieBreakQuestionAnswer, ProtoModels.VoteResultBallotTieBreakQuestionAnswer>();

        // write
        CreateMap<EnterVoteBallotResultsRequest, VoteBallotResultsEntered>();
        CreateMap<EnterVoteBallotQuestionResultRequest, VoteBallotQuestionResultsEventData>();
        CreateMap<EnterVoteTieBreakQuestionResultRequest, VoteTieBreakQuestionResultsEventData>();
        CreateMap<DefineVoteResultEntryParamsRequest, VoteResultEntryParams>();
        CreateMap<CreateUpdateVoteResultBallotQuestionAnswerRequest, VoteResultBallotQuestionAnswer>();
        CreateMap<CreateUpdateVoteResultBallotTieBreakQuestionAnswerRequest, VoteResultBallotTieBreakQuestionAnswer>();
        CreateMap<EnterVoteBallotResultsRequest, VoteBallotResults>();
        CreateMap<EnterVoteBallotQuestionResultRequest, VoteBallotQuestionResult>();
        CreateMap<EnterVoteTieBreakQuestionResultRequest, VoteTieBreakQuestionResult>();
        CreateMap<EnterVoteBallotResultsCountOfVotersRequest, VoteBallotResultsCountOfVoters>();
    }
}
