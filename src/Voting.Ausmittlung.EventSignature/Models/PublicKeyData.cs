// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.EventSignature.Models;

public class PublicKeyData
{
    public PublicKeyData(EcdsaPublicKey key, DateTime validFrom, DateTime validTo, DateTime? deletedAt)
    {
        Key = key;
        ValidFrom = validFrom;
        ValidTo = validTo;
        DeletedAt = deletedAt;
    }

    public EcdsaPublicKey Key { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }

    public DateTime? DeletedAt { get; }
}
