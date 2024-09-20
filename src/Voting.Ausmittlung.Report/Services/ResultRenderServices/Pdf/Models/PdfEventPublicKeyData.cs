// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature;

namespace Voting.Ausmittlung.Report.Services.ResultRenderServices.Pdf.Models;

public class PdfEventPublicKeyData
{
    public long? ExpectedSignedEventCount { get; set; }

    public long? ReadAtGenerationSignedEventCount { get; set; }

    public bool? HasMatchingSignedEventCount { get; set; }

    public PublicKeySignatureValidationResultType? SignatureValidationResultType { get; set; }

    public bool ShouldSerializeExpectedSignedEventCount() => ExpectedSignedEventCount.HasValue;

    public bool ShouldSerializeReadAtGenerationSignedEventCount() => ExpectedSignedEventCount.HasValue;

    public bool ShouldSerializeHasMatchingSignedEventCount() => ExpectedSignedEventCount.HasValue;
}
