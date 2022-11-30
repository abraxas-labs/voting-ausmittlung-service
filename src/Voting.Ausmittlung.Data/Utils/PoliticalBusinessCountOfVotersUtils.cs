// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
using System.Linq;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Data.Utils;

public static class PoliticalBusinessCountOfVotersUtils
{
    public static void AdjustCountOfVoters(
        PoliticalBusinessCountOfVoters target,
        PoliticalBusinessCountOfVoters other,
        int totalCountOfVoters,
        int deltaFactor = 1)
    {
        target.ConventionalReceivedBallots += other.ConventionalReceivedBallots * deltaFactor;
        target.ConventionalInvalidBallots += other.ConventionalInvalidBallots * deltaFactor;
        target.ConventionalBlankBallots += other.ConventionalBlankBallots * deltaFactor;
        target.ConventionalAccountedBallots += other.ConventionalAccountedBallots * deltaFactor;
        target.EVotingReceivedBallots += other.EVotingReceivedBallots * deltaFactor;
        target.EVotingInvalidBallots += other.EVotingInvalidBallots * deltaFactor;
        target.EVotingAccountedBallots += other.EVotingAccountedBallots * deltaFactor;
        target.UpdateVoterParticipation(totalCountOfVoters);
    }

    public static void AdjustCountOfVoters(
        PoliticalBusinessCountOfVoters target,
        PoliticalBusinessNullableCountOfVoters other,
        int totalCountOfVoters,
        int deltaFactor = 1)
    {
        AdjustCountOfVoters(
            target,
            other.MapToNonNullableSubTotal(),
            totalCountOfVoters,
            deltaFactor);
    }

    public static PoliticalBusinessCountOfVoters SumCountOfVoters(
        IReadOnlyCollection<PoliticalBusinessCountOfVoters> items,
        int totalCountOfVoters)
    {
        var sum = new PoliticalBusinessCountOfVoters
        {
            EVotingReceivedBallots = items.Sum(x => x.EVotingReceivedBallots),
            EVotingInvalidBallots = items.Sum(x => x.EVotingInvalidBallots),
            EVotingAccountedBallots = items.Sum(x => x.EVotingAccountedBallots),
            ConventionalReceivedBallots = items.Sum(x => x.ConventionalReceivedBallots),
            ConventionalInvalidBallots = items.Sum(x => x.ConventionalInvalidBallots),
            ConventionalBlankBallots = items.Sum(x => x.ConventionalBlankBallots),
            ConventionalAccountedBallots = items.Sum(x => x.ConventionalAccountedBallots),
        };

        sum.UpdateVoterParticipation(totalCountOfVoters);
        return sum;
    }
}
