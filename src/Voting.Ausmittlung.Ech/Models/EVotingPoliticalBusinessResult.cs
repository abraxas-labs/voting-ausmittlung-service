// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public abstract class EVotingPoliticalBusinessResult
{
    protected EVotingPoliticalBusinessResult(Guid politicalBusinessId, string basisCountingCircleId)
    {
        PoliticalBusinessId = politicalBusinessId;
        BasisCountingCircleId = basisCountingCircleId;
    }

    public Guid PoliticalBusinessId { get; }

    public string BasisCountingCircleId { get; }

    public PoliticalBusinessType PoliticalBusinessType { get; set; }

    public EVotingCountingCircleResultCountOfVotersInformation? CountOfVotersInformation { get; set; }
}
