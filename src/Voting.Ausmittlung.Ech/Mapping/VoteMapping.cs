// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0155_4_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class VoteMapping
{
    internal static VoteType ToEchVote(this Vote vote)
    {
        var voteDescriptionInfo = vote.Translations
            .Select(t => VoteDescriptionInfoType.Create(t.Language, t.ShortDescription))
            .ToList();
        var voteDescription = VoteDescriptionInformationType.Create(voteDescriptionInfo);
        return VoteType.Create(vote.Id.ToString(), vote.DomainOfInfluence.BasisDomainOfInfluenceId.ToString(), voteDescription);
    }
}
