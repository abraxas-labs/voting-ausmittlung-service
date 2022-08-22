// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public readonly struct EventSignatureDomainData
{
    public EventSignatureDomainData(Guid contestId)
    {
        ContestId = contestId;
    }

    public Guid ContestId { get; }
}
