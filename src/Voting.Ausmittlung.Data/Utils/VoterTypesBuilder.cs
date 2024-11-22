// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class VoterTypesBuilder
{
    public static List<VoterType> BuildEnabledVoterTypes(DomainOfInfluence? domainOfInfluence)
    {
        if (domainOfInfluence == null)
        {
            return new();
        }

        return BuildEnabledVoterTypes(new[] { domainOfInfluence });
    }

    public static List<VoterType> BuildEnabledVoterTypes(IReadOnlyCollection<DomainOfInfluence> domainOfInfluences)
    {
        if (domainOfInfluences.Count == 0)
        {
            return new();
        }

        var enabledVoterTypes = new List<VoterType>() { VoterType.Swiss };

        if (domainOfInfluences.Any(d => d.SwissAbroadVotingRight == SwissAbroadVotingRight.OnEveryCountingCircle))
        {
            enabledVoterTypes.Add(VoterType.SwissAbroad);
        }

        if (domainOfInfluences.Any(d => d.HasForeignerVoters))
        {
            enabledVoterTypes.Add(VoterType.Foreigner);
        }

        if (domainOfInfluences.Any(d => d.HasMinorVoters))
        {
            enabledVoterTypes.Add(VoterType.Minor);
        }

        return enabledVoterTypes;
    }
}
