// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public abstract class ElectionResult : CountingCircleResult
{
    public PoliticalBusinessNullableCountOfVoters CountOfVoters { get; set; } = new();

    public int CountOfBundlesNotReviewedOrDeleted { get; set; }

    // count of bundles not reviewed or deleted cannot be negative, since the implemented logic does not allow this
    public bool AllBundlesReviewedOrDeleted => CountOfBundlesNotReviewedOrDeleted == 0;

    public void UpdateVoterParticipation() => CountOfVoters.UpdateVoterParticipation(TotalCountOfVoters);
}
