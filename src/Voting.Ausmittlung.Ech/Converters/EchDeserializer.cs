// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Ech.Converters;

public static class EchDeserializer
{
    public static T FromXml<T>(string xml)
    {
        using var stringReader = new StringReader(xml);
        return Deserialize<T>(stringReader);
    }

    public static T FromXml<T>(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        return Deserialize<T>(streamReader);
    }

    private static T Deserialize<T>(TextReader textReader)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var xmlReader = XmlReader.Create(textReader);
        return (T?)serializer.Deserialize(xmlReader) ?? throw new ValidationException($"{nameof(EchDeserializer)}: Deserialization with given input returned null");
    }
}
