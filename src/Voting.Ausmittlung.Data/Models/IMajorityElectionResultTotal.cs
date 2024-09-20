// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IMajorityElectionResultTotal<out TInt>
{
    /// <summary>
    /// Gets the individual vote count.
    /// </summary>
    TInt IndividualVoteCount { get; }

    /// <summary>
    /// Gets the empty vote count.
    /// </summary>
    TInt EmptyVoteCount { get; }

    /// <summary>
    /// Gets the invalid vote count.
    /// </summary>
    TInt InvalidVoteCount { get; }

    /// <summary>
    /// Gets the total count of empty and invalid votes.
    /// </summary>
    int TotalEmptyAndInvalidVoteCount { get; }

    /// <summary>
    /// Gets the total count of candidate votes excl. individual votes.
    /// </summary>
    int TotalCandidateVoteCountExclIndividual { get; }

    /// <summary>
    /// Gets the total count of candidate votes incl. individual votes.
    /// </summary>
    int TotalCandidateVoteCountInclIndividual { get; }

    /// <summary>
    /// Gets the total count of votes incl. individual, empty and invalid votes.
    /// </summary>
    int TotalVoteCount { get; }
}
