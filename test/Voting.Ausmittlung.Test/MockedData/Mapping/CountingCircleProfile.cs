// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.Data.Models;
using DomainModels = Voting.Ausmittlung.Core.Domain;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class CountingCircleProfile : Profile
{
    public CountingCircleProfile()
    {
        CreateMap<ContestCountingCircleDetails, DomainModels.ContestCountingCircleDetails>();
        CreateMap<ContestCountingCircleElectorate, DomainModels.ContestCountingCircleElectorate>();
        CreateMap<CountOfVotersInformationSubTotal, DomainModels.CountOfVotersInformationSubTotal>();
        CreateMap<VotingCardResultDetail, DomainModels.VotingCardResultDetail>();
    }
}
