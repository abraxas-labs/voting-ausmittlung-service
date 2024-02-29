// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Requests;
using AutoMapper;
using Voting.Ausmittlung.Core.Domain;
using DataModels = Voting.Ausmittlung.Data.Models;
using ProtoModels = Abraxas.Voting.Ausmittlung.Services.V1.Models;

namespace Voting.Ausmittlung.Mapping;

public class ContestCountingCircleElectorateProfile : Profile
{
    public ContestCountingCircleElectorateProfile()
    {
        // read
        CreateMap<DataModels.ContestCountingCircleElectorateSummary, ProtoModels.ContestCountingCircleElectorateSummary>();
        CreateMap<DataModels.CountingCircleElectorateBase, ProtoModels.CountingCircleElectorate>();

        // write
        CreateMap<CreateUpdateContestCountingCircleElectorateRequest, ContestCountingCircleElectorate>();
    }
}
