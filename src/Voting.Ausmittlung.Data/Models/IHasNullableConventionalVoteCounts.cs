// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IHasNullableConventionalVoteCounts
{
    int? ConventionalVoteCount { get; set; }

    int EVotingExclWriteInsVoteCount { get; set; }

    int EVotingWriteInsVoteCount { get; set; }

    int EVotingInclWriteInsVoteCount { get; }

    int ECountingExclWriteInsVoteCount { get; set; }

    int ECountingWriteInsVoteCount { get; set; }

    int ECountingInclWriteInsVoteCount { get; }
}
