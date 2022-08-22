// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using eCH_0155_4_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ContestMapping
{
    internal static ContestType ToEchContest(this Contest contest)
    {
        var contestDescriptionInfos = contest.Translations
            .Select(t => ContestDescriptionInfo.Create(t.Language, t.Description))
            .ToList();
        var contestDescription = ContestDescriptionInformation.Create(contestDescriptionInfos);

        var eVotingPeriod = contest.EVoting
            ? EvotingPeriodType.Create(contest.EVotingFrom!.Value, contest.EVotingTo!.Value)
            : null;

        return ContestType.Create(contest.Id.ToString(), contest.Date, contestDescription, eVotingPeriod);
    }
}
