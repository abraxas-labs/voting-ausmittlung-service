// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public abstract class PoliticalBusinessResultImport
{
    protected PoliticalBusinessResultImport(Guid politicalBusinessId, Guid basisCountingCircleId, int totalCountOfVoters)
    {
        PoliticalBusinessId = politicalBusinessId;
        BasisCountingCircleId = basisCountingCircleId;
        CountOfVotersInformation = new CountingCircleResultCountOfVotersInformationImport(totalCountOfVoters);
    }

    public Guid PoliticalBusinessId { get; }

    public Guid BasisCountingCircleId { get; }

    public CountingCircleResultCountOfVotersInformationImport CountOfVotersInformation { get; }
}
