// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultSubTotal : IMajorityElectionResultSubTotal<int>, ISummableSubTotal<MajorityElectionResultSubTotal>
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

    public void Add(MajorityElectionResultSubTotal other, int deltaFactor = 1)
    {
        IndividualVoteCount += other.IndividualVoteCount * deltaFactor;
        InvalidVoteCount += other.InvalidVoteCount * deltaFactor;
        TotalCandidateVoteCountExclIndividual += other.TotalCandidateVoteCountExclIndividual * deltaFactor;
        EmptyVoteCountWriteIns += other.EmptyVoteCountWriteIns * deltaFactor;
        EmptyVoteCountExclWriteIns += other.EmptyVoteCountExclWriteIns * deltaFactor;
    }
}
