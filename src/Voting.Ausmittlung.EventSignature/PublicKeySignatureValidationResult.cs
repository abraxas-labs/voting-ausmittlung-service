// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature.Models;

namespace Voting.Ausmittlung.EventSignature;

public class PublicKeySignatureValidationResult
{
    public PublicKeySignatureValidationResult(PublicKeySignatureData signatureData, PublicKeyData keyData, bool isValid)
    {
        SignatureData = signatureData;
        KeyData = keyData;
        IsValid = isValid;
    }

    public PublicKeySignatureData SignatureData { get; }

    public PublicKeyData KeyData { get; }

    public bool IsValid { get; }
}
