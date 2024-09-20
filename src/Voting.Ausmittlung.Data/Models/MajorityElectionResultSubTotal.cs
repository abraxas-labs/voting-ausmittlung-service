// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultSubTotal : IMajorityElectionResultSubTotal<int>
{
    /// <inheritdoc />
    public int IndividualVoteCount { get; set; }

    /// <inheritdoc />
    public int EmptyVoteCountInclWriteIns => EmptyVoteCountWriteIns + EmptyVoteCountExclWriteIns;

    /// <inheritdoc />
    public int InvalidVoteCount { get; set; }

    /// <inheritdoc />
    public int TotalEmptyAndInvalidVoteCount => EmptyVoteCountInclWriteIns + InvalidVoteCount;

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual { get; set; }

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount;

    public int TotalVoteCount => TotalCandidateVoteCountInclIndividual + TotalEmptyAndInvalidVoteCount;

    public int EmptyVoteCountWriteIns { get; set; }

    public int EmptyVoteCountExclWriteIns { get; set; }
}
