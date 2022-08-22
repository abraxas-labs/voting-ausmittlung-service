// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Voting.Ausmittlung.Ech.Converters;

public static class EchSerializer
{
    private static readonly Encoding Encoding = new UTF8Encoding(false);

    private static readonly XmlWriterSettings XmlSettings = new XmlWriterSettings
    {
        Indent = false,
        NewLineOnAttributes = false,
        Encoding = Encoding,
    };

    public static void ToXml(PipeWriter writer, object o)
    {
        var serializer = new XmlSerializer(o.GetType());

        using var streamWriter = new StreamWriter(writer.AsStream(), Encoding);
        using var xmlWriter = XmlWriter.Create(streamWriter, XmlSettings);
        serializer.Serialize(xmlWriter, o);
    }
}
