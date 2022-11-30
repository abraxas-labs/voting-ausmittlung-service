// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature.Models;

namespace Voting.Ausmittlung.EventSignature;

public class PublicKeySignatureValidationResult
{
    public PublicKeySignatureValidationResult(PublicKeySignatureData? signatureData, PublicKeyData keyData, PublicKeySignatureValidationResultType type)
    {
        SignatureData = signatureData;
        KeyData = keyData;
        Type = type;
    }

    public PublicKeySignatureData? SignatureData { get; }

    public PublicKeyData KeyData { get; }

    public PublicKeySignatureValidationResultType Type { get; }

    public bool IsValid => Type == PublicKeySignatureValidationResultType.Valid;
}
