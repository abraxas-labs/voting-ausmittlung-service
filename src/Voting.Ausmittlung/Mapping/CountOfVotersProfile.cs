// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class CountOfVotersProfile : Profile
{
    public CountOfVotersProfile()
    {
        // read
        CreateMap<DataModels.PoliticalBusinessCountOfVoters, ProtoModels.PoliticalBusinessCountOfVoters>();
        CreateMap<DataModels.PoliticalBusinessNullableCountOfVoters, ProtoModels.PoliticalBusinessNullableCountOfVoters>();
        CreateMap<DataModels.VotingCardResultDetail, ProtoModels.VotingCardResultDetail>();
        CreateMap<DataModels.CountOfVotersInformationSubTotal, ProtoModels.CountOfVotersInformationSubTotal>();

        // wrtie
        CreateMap<EnterPoliticalBusinessCountOfVotersRequest, PoliticalBusinessCountOfVoters>();
    }
}
