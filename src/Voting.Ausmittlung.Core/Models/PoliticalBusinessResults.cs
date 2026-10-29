// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Models;

public class PoliticalBusinessResults
{
    public PoliticalBusinessResults(
        List<VoteResult> voteResults,
        List<ProportionalElectionResult> proportionalElectionResults,
        List<MajorityElectionResult> majorityElectionResults)
    {
        VoteResults = voteResults;
        ProportionalElectionResults = proportionalElectionResults;
        MajorityElectionResults = majorityElectionResults;
    }

    public List<VoteResult> VoteResults { get; }

    public List<ProportionalElectionResult> ProportionalElectionResults { get; }

    public List<MajorityElectionResult> MajorityElectionResults { get; }
}
