// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Ech0155_4_0;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Ech.Mapping;

internal static class ContestMapping
{
    internal static ContestType ToEchContest(this Contest contest)
    {
        var contestDescriptionInfos = contest.Translations
            .Select(t => new ContestDescriptionInformationTypeContestDescriptionInfo
            {
                Language = t.Language,
                ContestDescription = t.Description,
            })
            .ToList();

        var eVotingPeriod = contest.EVoting
            ? new EVotingPeriodType
            {
                EVotingPeriodFrom = contest.EVotingFrom!.Value,
                EVotingPeriodTill = contest.EVotingTo!.Value,
            }
            : null;

        return new ContestType
        {
            ContestIdentification = contest.Id.ToString(),
            ContestDate = contest.Date,
            ContestDescription = contestDescriptionInfos,
            EVotingPeriod = eVotingPeriod,
        };
    }
}
