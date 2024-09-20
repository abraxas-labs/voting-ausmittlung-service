// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public class ElectionResultImport : PoliticalBusinessResultImport
{
    internal ElectionResultImport(Guid politicalBusinessId, Guid basisCountingCircleId, CountingCircleResultCountOfVotersInformationImport countOfVotersInformationImport)
        : base(politicalBusinessId, basisCountingCircleId, countOfVotersInformationImport)
    {
    }

    public int CountOfVoters { get; internal set; }
}
