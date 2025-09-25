// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ContestCountingCircleDetailsProfile : Profile
{
    public ContestCountingCircleDetailsProfile()
    {
        // read
        CreateMap<DataModels.ContestCountingCircleDetails, ProtoModels.ContestCountingCircleDetails>()
            .ForMember(dst => dst.CountingCircleId, opts => opts.MapFrom(src => src.CountingCircle.BasisCountingCircleId));

        // write
        CreateMap<UpdateContestCountingCircleDetailsRequest, ContestCountingCircleDetails>();
        CreateMap<UpdateCountOfVotersInformationSubTotalRequest, CountOfVotersInformationSubTotal>();
        CreateMap<UpdateVotingCardResultDetailRequest, VotingCardResultDetail>();
        CreateMap<UpdateCountOfVotersInformationSubTotalRequest, CountOfVotersInformationSubTotal>();
    }
}
