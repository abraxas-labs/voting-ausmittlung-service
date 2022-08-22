// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ElectionResultImport : PoliticalBusinessResultImport
{
    internal ElectionResultImport(Guid politicalBusinessId, Guid basisCountingCircleId)
        : base(politicalBusinessId, basisCountingCircleId)
    {
    }

    public int CountOfVoters { get; internal set; }
}
