// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Security.Cryptography;
using Voting.Lib.Common;

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeySignatureCreateAuthenticationTagPayload
{
    public PublicKeySignatureCreateAuthenticationTagPayload(
        int signatureVersion,
        Guid contestId,
        string hostId,
        string keyId,
        byte[] publicKey,
        DateTime validFrom,
        DateTime validTo)
    {
        SignatureVersion = signatureVersion;
        ContestId = contestId;
        HostId = hostId;
        KeyId = keyId;
        PublicKey = publicKey;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public int SignatureVersion { get; }

    public Guid ContestId { get; }

    public string HostId { get; }

    public string KeyId { get; }

    public byte[] PublicKey { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }

    /// <summary>
    /// Converts the data of a public key creation to a signable byte payload.
    /// This should be signed with the private key stored in the host's memory.
    /// </summary>
    /// <returns>The byte payload of a public key creation.</returns>
    public byte[] ConvertToBytesToSign()
    {
        // changes here are event breaking and need another signature version.
        using var sha512 = SHA512.Create();

        using var byteConverter = new ByteConverter();
        return byteConverter
            .Append(SignatureVersion)
            .Append(ContestId.ToString().ToLower())
            .Append(HostId)
            .Append(KeyId)
            .Append(sha512.ComputeHash(PublicKey))
            .Append(ValidFrom)
            .Append(ValidTo)
            .GetBytes();
    }
}
