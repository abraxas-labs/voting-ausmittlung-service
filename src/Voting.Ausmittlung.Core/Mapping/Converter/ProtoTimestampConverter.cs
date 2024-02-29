// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;

namespace Voting.Ausmittlung.Core.Mapping.Converter;

public class ProtoTimestampConverter :
    ITypeConverter<Timestamp, DateTime>,
    ITypeConverter<Timestamp, DateTime?>,
    ITypeConverter<DateTime, Timestamp>,
    ITypeConverter<DateTime?, Timestamp?>
{
    public DateTime Convert(Timestamp source, DateTime destination, ResolutionContext context)
        => source.ToDateTime();

    public DateTime? Convert(Timestamp? source, DateTime? destination, ResolutionContext context)
        => source?.ToDateTime();

    public Timestamp Convert(DateTime source, Timestamp destination, ResolutionContext context)
        => source.ToTimestamp();

    public Timestamp? Convert(DateTime? source, Timestamp? destination, ResolutionContext context)
        => source?.ToTimestamp();
}
