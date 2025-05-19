// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Collections.Generic;
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
        foreach (var (targetSubTotal, otherSubTotal) in target.SubTotalsAsPairEnumerable(other))
        {
            targetSubTotal.ReceivedBallots += otherSubTotal.ReceivedBallots * deltaFactor;
            targetSubTotal.InvalidBallots += otherSubTotal.InvalidBallots * deltaFactor;
            targetSubTotal.BlankBallots += otherSubTotal.BlankBallots * deltaFactor;
            targetSubTotal.AccountedBallots += otherSubTotal.AccountedBallots * deltaFactor;
        }

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
        var sum = PoliticalBusinessCountOfVoters.CreateSum(items);
        sum.UpdateVoterParticipation(totalCountOfVoters);
        return sum;
    }
}
