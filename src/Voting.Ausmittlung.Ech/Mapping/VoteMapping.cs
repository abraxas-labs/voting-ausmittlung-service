// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Ech0155_4_0;
using Voting.Ausmittlung.Data.Models;
using VoteType = Ech0155_4_0.VoteType;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteMapping
{
    internal static VoteType ToEchVote(this Vote vote)
    {
        var voteDescriptionInfos = vote.Translations
            .Select(t => new VoteDescriptionInformationTypeVoteDescriptionInfo
            {
                Language = t.Language,
                VoteDescription = t.ShortDescription,
            })
            .ToList();

        return new VoteType
        {
            VoteIdentification = vote.Id.ToString(),
            DomainOfInfluenceIdentification = vote.DomainOfInfluence.BasisDomainOfInfluenceId.ToString(),
            VoteDescription = voteDescriptionInfos,
        };
    }
}
