// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        CreateMap<ContestCountingCircleDetails, DomainModels.ContestCountingCircleDetails>()
            .ForPath(dst => dst.CountOfVotersInformation.TotalCountOfVoters, opts => opts.MapFrom(src => src.TotalCountOfVoters))
            .ForPath(dst => dst.CountOfVotersInformation.SubTotalInfo, opts => opts.MapFrom(src => src.CountOfVotersInformationSubTotals));
        CreateMap<ContestCountingCircleElectorate, DomainModels.ContestCountingCircleElectorate>();
        CreateMap<CountOfVotersInformationSubTotal, DomainModels.CountOfVotersInformationSubTotal>();
        CreateMap<VotingCardResultDetail, DomainModels.VotingCardResultDetail>();
    }
}
