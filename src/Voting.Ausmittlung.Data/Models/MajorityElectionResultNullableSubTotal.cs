// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionResultNullableSubTotal : IMajorityElectionResultSubTotal<int?>, INullableSubTotal<MajorityElectionResultSubTotal>
{
    /// <inheritdoc />
    public int? IndividualVoteCount { get; set; }

    /// <inheritdoc />
    public int? EmptyVoteCount { get; set; }

    /// <inheritdoc />
    public int? InvalidVoteCount { get; set; }

    /// <inheritdoc />
    public int TotalCandidateVoteCountExclIndividual { get; set; }

    /// <inheritdoc />
    public int TotalCandidateVoteCountInclIndividual => TotalCandidateVoteCountExclIndividual + IndividualVoteCount.GetValueOrDefault();

    public MajorityElectionResultSubTotal MapToNonNullableSubTotal()
    {
        return new MajorityElectionResultSubTotal
        {
            IndividualVoteCount = IndividualVoteCount.GetValueOrDefault(),
            EmptyVoteCount = EmptyVoteCount.GetValueOrDefault(),
            InvalidVoteCount = InvalidVoteCount.GetValueOrDefault(),
            TotalCandidateVoteCountExclIndividual = TotalCandidateVoteCountExclIndividual,
        };
    }

    public void ReplaceNullValuesWithZero()
    {
        IndividualVoteCount ??= 0;
        EmptyVoteCount ??= 0;
        InvalidVoteCount ??= 0;
    }
}
