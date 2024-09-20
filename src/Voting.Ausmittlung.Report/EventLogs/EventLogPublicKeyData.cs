// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature;

namespace Voting.Ausmittlung.Report.EventLogs;

public class EventLogPublicKeyData
{
    public long? ExpectedSignedEventCount { get; set; }

    public long? ReadAtGenerationSignedEventCount { get; set; }

    public bool? HasMatchingSignedEventCount => ExpectedSignedEventCount == null || ReadAtGenerationSignedEventCount == null
        ? null
        : ExpectedSignedEventCount == ReadAtGenerationSignedEventCount;

    public PublicKeySignatureValidationResultType SignatureValidationResultType { get; set; }
}
