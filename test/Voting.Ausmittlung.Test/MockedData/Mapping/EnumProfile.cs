// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using AutoMapper.Extensions.EnumMapping;

namespace Voting.Ausmittlung.Test.MockedData.Mapping;

public class EnumProfile : Profile
{
    public EnumProfile()
    {
        CreateMap<EnumMockedData.TestEnum1, EnumMockedData.TestEnum2>()
            .ConvertUsingEnumMapping(opt => opt.MapByName())
            .ReverseMap();
    }
}
