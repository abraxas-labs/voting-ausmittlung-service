// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Models;

public abstract class VotingImportPoliticalBusinessResult
{
    protected VotingImportPoliticalBusinessResult(Guid politicalBusinessId, string basisCountingCircleId)
    {
        PoliticalBusinessId = politicalBusinessId;
        BasisCountingCircleId = basisCountingCircleId;
    }

    public Guid PoliticalBusinessId { get; }

    /// <summary>
    /// Gets the basis counting circle id.
    /// Can contain non-guids for "TestUrnen" from swiss-post.
    /// </summary>
    public string BasisCountingCircleId { get; }

    public PoliticalBusinessType PoliticalBusinessType { get; set; }

    public int TotalCountOfVoters { get; set; }
}
