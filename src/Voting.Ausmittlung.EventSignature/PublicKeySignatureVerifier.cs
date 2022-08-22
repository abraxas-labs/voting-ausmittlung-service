// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.EventSignature;

/// <summary>
/// A signature verifier to verify that the public key is created from a trusted source.
/// </summary>
public class PublicKeySignatureVerifier
{
    private readonly IPkcs11DeviceAdapter _pkcs11DeviceAdapter;

    public PublicKeySignatureVerifier(IPkcs11DeviceAdapter pkcs11DeviceAdapter)
    {
        _pkcs11DeviceAdapter = pkcs11DeviceAdapter;
    }

    /// <summary>
    /// Verifies that the signature is created from a trusted source.
    /// </summary>
    /// <param name="signatureData">Public key signature data.</param>
    /// <param name="keyData">Public Key data.</param>
    /// <returns>The public key signature validation result.</returns>
    public PublicKeySignatureValidationResult VerifySignature(PublicKeySignatureData signatureData, PublicKeyData keyData)
    {
        var payload = new PublicKeySignaturePayload(
            signatureData.SignatureVersion,
            signatureData.ContestId,
            signatureData.HostId,
            keyData.Key.Id,
            keyData.Key.PublicKey,
            keyData.ValidFrom,
            keyData.ValidTo);

        var isValid = _pkcs11DeviceAdapter.VerifySignature(payload.ConvertToBytesToSign(), signatureData.HsmSignature);
        return new PublicKeySignatureValidationResult(signatureData, keyData, isValid);
    }
}
