// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Extensions;

public static class ContestCountingCircleDetailsExtensions
{
    public static int GetTotalCountOfVotersForDomainOfInfluence(this ContestCountingCircleDetails details, DomainOfInfluence domainOfInfluence)
    {
        return details.CountOfVotersInformationSubTotals
            .Where(x => x.VoterType == VoterType.Swiss
                || (domainOfInfluence.SwissAbroadVotingRight == SwissAbroadVotingRight.OnEveryCountingCircle && x.VoterType == VoterType.SwissAbroad)
                || (domainOfInfluence.HasForeignerVoters && x.VoterType == VoterType.Foreigner)
                || (domainOfInfluence.HasMinorVoters && x.VoterType == VoterType.Minor))
            .Sum(x => x.CountOfVoters.GetValueOrDefault());
    }
}
