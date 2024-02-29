// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.EventSignature;

public class ContestCacheEntry
{
    public Guid Id { get; set; }

    public ContestState State { get; set; }

    public DateTime Date { get; set; }

    public DateTime PastLockedPer { get; set; }

    public ContestCacheEntryKeyData? KeyData { get; set; }
}
