// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Security.Cryptography;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeySignatureDeleteHsmPayload
{
    public PublicKeySignatureDeleteHsmPayload(
        PublicKeySignatureDeleteAuthenticationTagPayload authenticationTagPayload,
        byte[] authenticationTag)
        : this(
            authenticationTagPayload.SignatureVersion,
            authenticationTagPayload.ContestId,
            authenticationTagPayload.HostId,
            authenticationTagPayload.KeyId,
            authenticationTagPayload.DeletedAt,
            authenticationTagPayload.SignedEventCount,
            authenticationTag)
    {
    }

    public PublicKeySignatureDeleteHsmPayload(
        int signatureVersion,
        Guid contestId,
        string hostId,
        string keyId,
        DateTime deletedAt,
        long signedEventCount,
        byte[] authenticationTag)
    {
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        KeyId = keyId;
        DeletedAt = deletedAt;
        SignedEventCount = signedEventCount;
        AuthenticationTag = authenticationTag;
    }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public string KeyId { get; }

    public DateTime DeletedAt { get; }

    public long SignedEventCount { get; }

    public byte[] AuthenticationTag { get; }

    /// <summary>
    /// Converts the data of a public key deletion to a signable byte payload.
    /// This should be signed by the HSM.
    /// </summary>
    /// <returns>The byte payload of a public key deletion.</returns>
    public byte[] ConvertToBytesToSign()
    {
        // changes here are event breaking and need another signature version.
        using var sha512 = SHA512.Create();
        using var byteConverter = new ByteConverter();
        return byteConverter
            .Append(SignatureVersion)
            .Append(ContestId.ToString())
            .Append(HostId)
            .Append(KeyId)
            .Append(DeletedAt)
            .Append(SignedEventCount)
            .Append(sha512.ComputeHash(AuthenticationTag))
            .GetBytes();
    }
}
