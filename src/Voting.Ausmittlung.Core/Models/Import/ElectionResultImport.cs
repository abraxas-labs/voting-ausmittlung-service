// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ElectionResultImport : PoliticalBusinessResultImport
{
    internal ElectionResultImport(Guid politicalBusinessId, Guid basisCountingCircleId, int totalCountOfVoters)
        : base(politicalBusinessId, basisCountingCircleId, totalCountOfVoters)
    {
    }

    public int CountOfVoters { get; internal set; }
}
