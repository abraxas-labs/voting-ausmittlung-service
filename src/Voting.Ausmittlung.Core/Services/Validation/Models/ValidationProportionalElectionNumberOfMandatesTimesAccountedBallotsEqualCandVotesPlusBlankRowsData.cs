// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationProportionalElectionNumberOfMandatesTimesAccountedBallotsEqualCandVotesPlusBlankRowsData
{
    public int BlankRowsCount { get; set; }

    public int CandidateVotes { get; set; }

    public int NumberOfMandates { get; set; }

    public int TotalAccountedBallots { get; set; }

    public int SumVoteCount { get; set; }

    public int NumberOfMandatesTimesAccountedBallots { get; set; }
}
