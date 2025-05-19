// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class CountOfVotersProfile : Profile
{
    public CountOfVotersProfile()
    {
        CreateMap<EnterPoliticalBusinessCountOfVotersRequest, PoliticalBusinessNullableCountOfVoters>()
            .ForPath(dst => dst.ConventionalSubTotal.AccountedBallots, opt => opt.MapFrom(src => src.ConventionalAccountedBallots))
            .ForPath(dst => dst.ConventionalSubTotal.BlankBallots, opt => opt.MapFrom(src => src.ConventionalBlankBallots))
            .ForPath(dst => dst.ConventionalSubTotal.InvalidBallots, opt => opt.MapFrom(src => src.ConventionalInvalidBallots))
            .ForPath(dst => dst.ConventionalSubTotal.ReceivedBallots, opt => opt.MapFrom(src => src.ConventionalReceivedBallots));
    }
}
