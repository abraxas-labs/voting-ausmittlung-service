// (c) Copyright by Abraxas Informatik AG
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

    /// <summary>
    /// Gets the count of signed business events which the corresponding <see cref="Key"/> signs.
    /// </summary>
    public long SignedEventCount { get; private set; }

    /// <summary>
    /// Increments the <see cref="SignedEventCount"/> by 1. This count is used to verify how many events a specific <see cref="Key"/> has signed.
    /// </summary>
    public void IncrementSignedEventCount()
    {
        SignedEventCount++;
    }
}
