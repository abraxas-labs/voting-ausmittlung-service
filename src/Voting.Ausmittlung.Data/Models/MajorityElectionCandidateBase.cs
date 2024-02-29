// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public abstract class MajorityElectionCandidateBase : ElectionCandidate
{
    public string Description => $"{Number} {PoliticalLastName} {PoliticalFirstName}";

    public abstract string Party { get; }

    public abstract Guid PoliticalBusinessId { get; }
}
