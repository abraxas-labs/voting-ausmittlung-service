// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System.Linq;
using Voting.Ausmittlung.BiproportionalApportionment.TieAndTransfer;

namespace Voting.Ausmittlung.Core.Utils.DoubleProportional.Models;

public class SaintLagueAlgorithmResult
{
    public SaintLagueAlgorithmResult(
        decimal[] quotients,
        int[] apportionment,
        TieState[] tieStates,
        int countOfMissingNumberOfMandates,
        decimal electionKey)
    {
        Quotients = quotients;
        Apportionment = apportionment;
        TieStates = tieStates;
        CountOfMissingNumberOfMandates = countOfMissingNumberOfMandates;
        ElectionKey = electionKey;
    }

    public decimal[] Quotients { get; }

    public int[] Apportionment { get; }

    public TieState[] TieStates { get; }

    public int CountOfMissingNumberOfMandates { get; }

    public decimal ElectionKey { get; }

    public bool HasTies => TieStates.Any(t => t != TieState.Unique);
}
