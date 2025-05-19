// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class ProportionalElectionCandidateResultSubTotal : IProportionalElectionCandidateResultTotal, ISummableSubTotal<ProportionalElectionCandidateResultSubTotal>
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

    public void Add(ProportionalElectionCandidateResultSubTotal other, int deltaFactor = 1)
    {
        UnmodifiedListVotesCount += other.UnmodifiedListVotesCount;
        ModifiedListVotesCount += other.ModifiedListVotesCount;
        CountOfVotesOnOtherLists += other.CountOfVotesOnOtherLists;
        CountOfVotesFromAccumulations += other.CountOfVotesFromAccumulations;
    }
}
