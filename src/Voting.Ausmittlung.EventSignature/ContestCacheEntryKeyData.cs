// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Cryptography.Asymmetric;

namespace Voting.Ausmittlung.EventSignature;

public class ContestCacheEntryKeyData
{
    public ContestCacheEntryKeyData(EcdsaPrivateKey key, DateTime validFrom, DateTime validTo)
    {
        Key = key;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public EcdsaPrivateKey Key { get; }

    public DateTime ValidFrom { get; }

    public DateTime ValidTo { get; }
}
