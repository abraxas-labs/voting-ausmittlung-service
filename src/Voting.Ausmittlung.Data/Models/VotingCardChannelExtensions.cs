// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;

namespace Voting.Ausmittlung.Data.Models;

public static class VotingCardChannelExtensions
{
    public static IEnumerable<T> OrderByPriority<T>(this IEnumerable<T> enumerable)
        where T : IVotingCardChannel
    {
        return enumerable
                .OrderBy(x => x.GetPriority())
                .ThenBy(x => x.Channel)
                .ThenBy(x => !x.Valid);
    }

    // priorities defined by the business team
    private static int GetPriority(this IVotingCardChannel vcChannel)
        => vcChannel.Channel switch
        {
            VotingChannel.BallotBox => 0,
            VotingChannel.Paper => 1,
            VotingChannel.ByMail => 2,
            VotingChannel.EVoting => 3,
            _ => 9,
        };
}
