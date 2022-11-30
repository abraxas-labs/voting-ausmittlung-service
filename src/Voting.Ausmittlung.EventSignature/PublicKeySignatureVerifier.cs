// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.EventSignature.Models;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.EventSignature;

/// <summary>
/// A signature verifier to verify that the public key is created from a trusted source.
/// </summary>
public class PublicKeySignatureVerifier
{
    private readonly IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> _asymmetricAlgorithmAdapter;
    private readonly IPkcs11DeviceAdapter _pkcs11DeviceAdapter;

    public PublicKeySignatureVerifier(IPkcs11DeviceAdapter pkcs11DeviceAdapter, IAsymmetricAlgorithmAdapter<EcdsaPublicKey, EcdsaPrivateKey> asymmetricAlgorithmAdapter)
    {
        _pkcs11DeviceAdapter = pkcs11DeviceAdapter;
        _asymmetricAlgorithmAdapter = asymmetricAlgorithmAdapter;
    }

    /// <summary>
    /// Verifies that the public key is created and deleted from a trusted source.
    /// </summary>
    /// <param name="signatureCreateData">Public key signature create data.</param>
    /// <param name="signatureDeleteData">Public key signature delete data.</param>
    /// <param name="keyData">Public Key data.</param>
    /// <returns>The public key signature validation result.</returns>
    public PublicKeySignatureValidationResult VerifySignature(PublicKeySignatureCreateData signatureCreateData, PublicKeySignatureDeleteData? signatureDeleteData, PublicKeyData keyData)
    {
        var signatureData = BuildSignatureData(signatureCreateData, signatureDeleteData);

        var resultType = signatureData != null
            ? GetResultType(signatureCreateData, signatureDeleteData, keyData)
            : PublicKeySignatureValidationResultType.CreateDeletePropertiesMismatch;

        return new PublicKeySignatureValidationResult(signatureData, keyData, resultType);
    }

    private PublicKeySignatureData? BuildSignatureData(PublicKeySignatureCreateData signatureCreateData, PublicKeySignatureDeleteData? signatureDeleteData)
    {
        var signatureData = new PublicKeySignatureData(signatureCreateData.SignatureVersion, signatureCreateData.HostId);

        if (signatureDeleteData == null)
        {
            return signatureData;
        }

        var hasMatchingProperties = signatureCreateData.SignatureVersion == signatureDeleteData.SignatureVersion
            && signatureCreateData.ContestId == signatureDeleteData.ContestId
            && signatureCreateData.KeyId.Equals(signatureDeleteData.KeyId, StringComparison.Ordinal)
            && signatureCreateData.HostId.Equals(signatureDeleteData.HostId, StringComparison.Ordinal);

        return hasMatchingProperties
            ? signatureData
            : null;
    }

    private bool AuthenticationTagCreateIsValid(PublicKeySignatureCreateAuthenticationTagPayload authenticationTagPayload, byte[] authenticationTag, EcdsaPublicKey key)
    {
        return _asymmetricAlgorithmAdapter.VerifySignature(
            authenticationTagPayload.ConvertToBytesToSign(),
            authenticationTag,
            key);
    }

    private bool AuthenticationTagDeleteIsValid(PublicKeySignatureDeleteAuthenticationTagPayload authenticationTagPayload, byte[] authenticationTag, EcdsaPublicKey key)
    {
        return _asymmetricAlgorithmAdapter.VerifySignature(
            authenticationTagPayload.ConvertToBytesToSign(),
            authenticationTag,
            key);
    }

    private bool HsmSignatureCreateIsValid(PublicKeySignatureCreateHsmPayload hsmCreatePayload, byte[] hsmSignature)
    {
        return _pkcs11DeviceAdapter.VerifySignature(hsmCreatePayload.ConvertToBytesToSign(), hsmSignature);
    }

    private bool HsmSignatureDeleteIsValid(PublicKeySignatureDeleteHsmPayload hsmCreatePayload, byte[] hsmSignature)
    {
        return _pkcs11DeviceAdapter.VerifySignature(hsmCreatePayload.ConvertToBytesToSign(), hsmSignature);
    }

    private PublicKeySignatureValidationResultType GetResultType(PublicKeySignatureCreateData signatureCreateData, PublicKeySignatureDeleteData? signatureDeleteData, PublicKeyData keyData)
    {
        var authTagCreatePayload = new PublicKeySignatureCreateAuthenticationTagPayload(
            signatureCreateData.SignatureVersion,
            signatureCreateData.ContestId,
            signatureCreateData.HostId,
            keyData.Key.Id,
            keyData.Key.PublicKey,
            keyData.ValidFrom,
            keyData.ValidTo);

        if (!AuthenticationTagCreateIsValid(authTagCreatePayload, signatureCreateData.AuthenticationTag, keyData.Key))
        {
            return PublicKeySignatureValidationResultType.AuthenticationTagCreateInvalid;
        }

        var hsmCreatePayload = new PublicKeySignatureCreateHsmPayload(
            authTagCreatePayload,
            signatureCreateData.AuthenticationTag);

        if (!HsmSignatureCreateIsValid(hsmCreatePayload, signatureCreateData.HsmSignature))
        {
            return PublicKeySignatureValidationResultType.HsmSignatureCreateInvalid;
        }

        if (signatureDeleteData == null)
        {
            return PublicKeySignatureValidationResultType.Valid;
        }

        var authTagDeletePayload = new PublicKeySignatureDeleteAuthenticationTagPayload(
            signatureDeleteData.SignatureVersion,
            signatureDeleteData.ContestId,
            signatureDeleteData.HostId,
            keyData.Key.Id,
            keyData.DeletedAt!.Value,
            signatureDeleteData.SignedEventCount);

        if (!AuthenticationTagDeleteIsValid(authTagDeletePayload, signatureDeleteData.AuthenticationTag, keyData.Key))
        {
            return PublicKeySignatureValidationResultType.AuthenticationTagDeleteInvalid;
        }

        var hsmDeletePayload = new PublicKeySignatureDeleteHsmPayload(
            authTagDeletePayload,
            signatureDeleteData.AuthenticationTag);

        if (!HsmSignatureDeleteIsValid(hsmDeletePayload, signatureDeleteData.HsmSignature))
        {
            return PublicKeySignatureValidationResultType.HsmSignatureDeleteInvalid;
        }

        return PublicKeySignatureValidationResultType.Valid;
    }
}
