// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Domain;

public readonly struct EventSignatureBusinessDomainData
{
    public EventSignatureBusinessDomainData(Guid contestId)
    {
        ContestId = contestId;
    }

    public Guid ContestId { get; }
}
