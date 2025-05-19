// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public static class MajorityElectionCandidateEndResultStateExtensions
{
    public static bool IsEligible(this MajorityElectionCandidateEndResultState state)
    {
        return state != MajorityElectionCandidateEndResultState.NotEligible
            && state != MajorityElectionCandidateEndResultState.NotElectedInPrimaryElectionNotEligible
            && state != MajorityElectionCandidateEndResultState.AbsoluteMajorityAndNotElectedInPrimaryElectionNotEligible;
    }
}
