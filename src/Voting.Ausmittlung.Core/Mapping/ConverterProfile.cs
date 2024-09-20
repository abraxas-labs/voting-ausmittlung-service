// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using AutoMapper;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Voting.Ausmittlung.Core.Mapping.Converter;

namespace Voting.Ausmittlung.Core.Mapping;

public class ConverterProfile : Profile
{
    public ConverterProfile()
    {
        CreateMap<Timestamp, DateTime>().ConvertUsing<ProtoTimestampConverter>();
        CreateMap<Timestamp, DateTime?>().ConvertUsing<ProtoTimestampConverter>();
        CreateMap<DateTime, Timestamp>().ConvertUsing<ProtoTimestampConverter>();
        CreateMap<DateTime?, Timestamp?>().ConvertUsing<ProtoTimestampConverter>();

        CreateMap<DateTime, DateOnly>().ConvertUsing<DateTimeDateOnlyConverter>();
        CreateMap<DateTime?, DateOnly?>().ConvertUsing<DateTimeDateOnlyConverter>();
        CreateMap<DateOnly, DateTime>().ConvertUsing<DateTimeDateOnlyConverter>();
        CreateMap<DateOnly?, DateTime?>().ConvertUsing<DateTimeDateOnlyConverter>();

        CreateMap<Guid?, string>().ConvertUsing<GuidStringConverter>();
        CreateMap<Guid, string>().ConvertUsing<GuidStringConverter>();
        CreateMap<string, Guid?>().ConvertUsing<GuidStringConverter>();
        CreateMap<string, Guid>().ConvertUsing<GuidStringConverter>();

        CreateMap(typeof(IEnumerable<>), typeof(RepeatedField<>))
            .ConvertUsing(typeof(RepeatedFieldConverter<,>));

        CreateMap(typeof(RepeatedField<>), typeof(RepeatedField<>))
            .ConvertUsing(typeof(RepeatedFieldConverter<,>));

        CreateMap<byte[], ByteString>().ConvertUsing<ByteStringConverter>();
    }
}
