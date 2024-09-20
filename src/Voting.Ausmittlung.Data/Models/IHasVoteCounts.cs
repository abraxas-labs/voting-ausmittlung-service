// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IHasVoteCounts
{
    int ConventionalVoteCount { get; set; }

    int EVotingVoteCount { get; set; }
}
