// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public abstract class PoliticalBusinessResultImport
{
    protected PoliticalBusinessResultImport(Guid politicalBusinessId, Guid basisCountingCircleId)
    {
        PoliticalBusinessId = politicalBusinessId;
        BasisCountingCircleId = basisCountingCircleId;
    }

    public Guid PoliticalBusinessId { get; }

    public Guid BasisCountingCircleId { get; }
}
