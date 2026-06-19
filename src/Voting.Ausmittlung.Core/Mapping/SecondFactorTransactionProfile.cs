// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Voting.Ausmittlung.TemporaryData.Models;

namespace Voting.Ausmittlung.Core.Mapping;

public class SecondFactorTransactionProfile : Profile
{
    public SecondFactorTransactionProfile()
    {
        CreateMap<Lib.Iam.SecondFactor.Models.SecondFactorTransaction, SecondFactorTransaction>().ReverseMap();
    }
}
