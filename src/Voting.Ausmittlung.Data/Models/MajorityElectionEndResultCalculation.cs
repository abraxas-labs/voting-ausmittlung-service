// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class MajorityElectionEndResultCalculation
{
    public int? DecisiveVoteCount { get; set; }

    public decimal? AbsoluteMajorityThreshold { get; set; }

    public int? AbsoluteMajority { get; set; }
}
