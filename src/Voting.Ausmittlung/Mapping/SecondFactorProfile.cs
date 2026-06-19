// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Abraxas.Voting.Ausmittlung.Services.V1.Models;
using AutoMapper;
using AutoMapper.Extensions.EnumMapping;
using Voting.Lib.Iam.SecondFactor.Models;
using Voting.Lib.Iam.Services.ApiClient.Identity;
using SecondFactorTransaction = Abraxas.Voting.Ausmittlung.Services.V1.Models.SecondFactorTransaction;

namespace Voting.Ausmittlung.Mapping;

public class SecondFactorProfile : Profile
{
    public SecondFactorProfile()
    {
        CreateMap<V1SecondFactorProvider, SecondFactorTransactionProvider>()
            .ConvertUsingEnumMapping(x => x.MapByName(true));
        CreateMap<SecondFactorTransactionInfo, SecondFactorTransaction>()
            .ForMember(dst => dst.Id, opts => opts.MapFrom(src => src.Transaction.Id))
            .ForPath(dst => dst.Nevis.CorrelationCode, opts =>
            {
                opts.Condition(x => x.Source.AvailableProviders!.Contains(V1SecondFactorProvider.NEVIS));
                opts.MapFrom(src => src.CorrelationCode);
            })
            .ForPath(dst => dst.Nevis.QrCode, opts =>
            {
                opts.Condition(x => x.Source.AvailableProviders!.Contains(V1SecondFactorProvider.NEVIS));
                opts.MapFrom(src => src.Nevis!.QrCode);
            });
    }
}
