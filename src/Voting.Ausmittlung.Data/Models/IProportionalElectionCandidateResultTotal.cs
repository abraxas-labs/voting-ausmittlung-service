// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IProportionalElectionCandidateResultTotal
{
    /// <summary>
    /// Gets the count of votes gained from unmodified lists.
    /// </summary>
    int UnmodifiedListVotesCount { get; }

    /// <summary>
    /// Gets the count of votes gained from modified lists.
    /// </summary>
    int ModifiedListVotesCount { get; }

    /// <summary>
    /// Gets the count of candidate votes gained from "other" lists (panaschieren).
    /// </summary>
    int CountOfVotesOnOtherLists { get; }

    /// <summary>
    /// Gets the count of votes gained from accumulating this candidate (kumulieren).
    /// </summary>
    int CountOfVotesFromAccumulations { get; }

    /// <summary>
    /// Gets the total count of votes (unmodified + modified).
    /// </summary>
    int VoteCount { get; }
}
