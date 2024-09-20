// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationMajorityElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusEmptyPlusInvalidVotesData
{
    public int EmptyVoteCount { get; set; }

    public int InvalidVoteCount { get; set; }

    public int CandidateVotesInclIndividual { get; set; }

    public int NumberOfMandates { get; set; }

    public int TotalAccountedBallots { get; set; }

    public int SumVoteCount { get; set; }

    public int NumberOfMandatesTimesAccountedBallots { get; set; }
}
