// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Models.Import;

public abstract class PoliticalBusinessResultImport
{
    protected PoliticalBusinessResultImport(Guid politicalBusinessId, Guid basisCountingCircleId, CountingCircleResultCountOfVotersInformationImport countOfVotersInformation)
    {
        PoliticalBusinessId = politicalBusinessId;
        BasisCountingCircleId = basisCountingCircleId;
        CountOfVotersInformation = countOfVotersInformation;
    }

    public Guid PoliticalBusinessId { get; }

    public Guid BasisCountingCircleId { get; }

    public CountingCircleResultCountOfVotersInformationImport CountOfVotersInformation { get; }
}
