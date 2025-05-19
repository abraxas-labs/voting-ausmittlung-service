// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultNullableSubTotal : IMajorityElectionResultSubTotal<int?>, INullableSubTotal<MajorityElectionResultSubTotal>, ISummableSubTotal<MajorityElectionResultSubTotal>
{
    /// <inheritdoc />
    public int? IndividualVoteCount { get; set; }

    /// <inheritdoc />
    public int? EmptyVoteCountInclWriteIns => EmptyVoteCountWriteIns.GetValueOrDefault() + EmptyVoteCountExclWriteIns.GetValueOrDefault();

    /// <inheritdoc />
    public int? InvalidVoteCount { get; set; }

    /// <inheritdoc />
    public int TotalEmptyAndInvalidVoteCount => EmptyVoteCountInclWriteIns.GetValueOrDefault() + InvalidVoteCount.GetValueOrDefault();

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual { get; set; }

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount.GetValueOrDefault();

    /// <inheritdoc />
    public int TotalVoteCount => TotalCandidateVoteCountInclIndividual + TotalEmptyAndInvalidVoteCount;

    public int? EmptyVoteCountWriteIns { get; set; }

    public int? EmptyVoteCountExclWriteIns { get; set; }

    public MajorityElectionResultSubTotal MapToNonNullableSubTotal()
    {
        return new MajorityElectionResultSubTotal
        {
            IndividualVoteCount = IndividualVoteCount.GetValueOrDefault(),
            EmptyVoteCountWriteIns = EmptyVoteCountWriteIns.GetValueOrDefault(),
            EmptyVoteCountExclWriteIns = EmptyVoteCountExclWriteIns.GetValueOrDefault(),
            InvalidVoteCount = InvalidVoteCount.GetValueOrDefault(),
            TotalCandidateVoteCountExclIndividual = TotalCandidateVoteCountExclIndividual,
        };
    }

    public void ReplaceNullValuesWithZero()
    {
        IndividualVoteCount ??= 0;
        EmptyVoteCountWriteIns ??= 0;
        EmptyVoteCountExclWriteIns ??= 0;
        InvalidVoteCount ??= 0;
    }

    public void Add(MajorityElectionResultSubTotal other, int deltaFactor = 1)
    {
        ReplaceNullValuesWithZero();
        IndividualVoteCount += other.IndividualVoteCount * deltaFactor;
        EmptyVoteCountWriteIns += other.EmptyVoteCountWriteIns * deltaFactor;
        EmptyVoteCountExclWriteIns += other.EmptyVoteCountExclWriteIns * deltaFactor;
        InvalidVoteCount += other.InvalidVoteCount * deltaFactor;
        TotalCandidateVoteCountExclIndividual += other.TotalCandidateVoteCountExclIndividual * deltaFactor;
    }
}
