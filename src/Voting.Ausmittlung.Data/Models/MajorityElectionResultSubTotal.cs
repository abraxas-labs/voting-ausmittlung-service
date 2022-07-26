// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultSubTotal : IMajorityElectionResultSubTotal<int>
{
    /// <inheritdoc />
    public int IndividualVoteCount { get; set; }

    /// <inheritdoc />
    public int EmptyVoteCount { get; set; }

    /// <inheritdoc />
    public int InvalidVoteCount { get; set; }

    /// <inheritdoc />
    public int TotalEmptyAndInvalidVoteCount => EmptyVoteCount + InvalidVoteCount;

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual { get; set; }

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount;

    public int TotalVoteCount => TotalCandidateVoteCountInclIndividual + TotalEmptyAndInvalidVoteCount;
}
