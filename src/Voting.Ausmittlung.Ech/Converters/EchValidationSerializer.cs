// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
using Voting.Lib.Common;
using Voting.Lib.Ech;

namespace Voting.Ausmittlung.Ech.Converters;

public class EchValidationSerializer
{
    private readonly EchSerializer _echSerializer;

    public EchValidationSerializer(EchSerializer echSerializer)
    {
        _echSerializer = echSerializer;
    }

    public async Task SerializeEchToXml<T>(Stream targetStream, T data, XmlSchemaSet schemaSet, XmlAttributeOverrides? xmlAttributeOverrides = null, CancellationToken ct = default)
    {
        await using var xmlValidationStream = new XmlValidationOnWriteStream(targetStream, schemaSet);

        // WriteXml is synchronous and may write into a Pipe. The concurrent ValidateXml task
        // reads from the other end of that Pipe. If WriteXml ran on the current thread it
        // could deadlock once the Pipe's internal buffer fills up, because the reader
        // (an async Task) would never get a chance to drain it. Task.Run offloads the
        // blocking serialization to a thread-pool thread so both sides of the Pipe can
        // make progress concurrently.
        await Task.Run(() => _echSerializer.WriteXml(xmlValidationStream, data, xmlAttributeOverrides, null, true), ct);
        await xmlValidationStream.WaitForValidation();
    }
}
