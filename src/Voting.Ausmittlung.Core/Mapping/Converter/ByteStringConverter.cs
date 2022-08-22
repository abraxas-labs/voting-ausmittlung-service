// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using AutoMapper;
using Google.Protobuf;

namespace Voting.Ausmittlung.Core.Mapping.Converter;

public class ByteStringConverter : ITypeConverter<byte[], ByteString>
{
    public ByteString Convert(byte[] source, ByteString destination, ResolutionContext context)
    {
        return ByteString.CopyFrom(source);
    }
}
