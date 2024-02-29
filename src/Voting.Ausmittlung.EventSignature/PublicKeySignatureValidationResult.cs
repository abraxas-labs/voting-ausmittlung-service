// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature.Models;

namespace Voting.Ausmittlung.EventSignature;

public class PublicKeySignatureValidationResult
{
    public PublicKeySignatureValidationResult(
        PublicKeySignatureData? signatureData,
        PublicKeyData keyData,
        PublicKeySignatureValidationResultType createPublicKeySignatureValidationResultType,
        PublicKeySignatureValidationResultType? deletePublicKeySignatureValidationResultType)
    {
        SignatureData = signatureData;
        KeyData = keyData;
        CreatePublicKeySignatureValidationResultType = createPublicKeySignatureValidationResultType;
        DeletePublicKeySignatureValidationResultType = deletePublicKeySignatureValidationResultType;
    }

    /// <summary>
    /// Gets the signature data from the key. If it is null, then there was a mismatch of the host or signature version
    /// of the create and delete key event.
    /// </summary>
    public PublicKeySignatureData? SignatureData { get; }

    public PublicKeyData KeyData { get; }

    public PublicKeySignatureValidationResultType CreatePublicKeySignatureValidationResultType { get; }

    public PublicKeySignatureValidationResultType? DeletePublicKeySignatureValidationResultType { get; }

    public bool IsValid =>
        SignatureData != null &&
        CreatePublicKeySignatureValidationResultType == PublicKeySignatureValidationResultType.Valid &&
        (DeletePublicKeySignatureValidationResultType == null || DeletePublicKeySignatureValidationResultType == PublicKeySignatureValidationResultType.Valid);
}
