// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidateResultSubTotal : IProportionalElectionCandidateResultTotal
{
    /// <inheritdoc />
    public int UnmodifiedListVotesCount { get; set; }

    /// <inheritdoc />
    public int ModifiedListVotesCount { get; set; }

    /// <inheritdoc />
    public int CountOfVotesOnOtherLists { get; set; }

    /// <inheritdoc />
    public int CountOfVotesFromAccumulations { get; set; }

    /// <inheritdoc />
    public int VoteCount => UnmodifiedListVotesCount + ModifiedListVotesCount;
}
