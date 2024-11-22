// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Ech.Utils;

public static class DeliveryHeaderUtils
{
    public static string EnrichComment(string comment, bool testingPhaseEnded)
    {
        return $"{comment} / {(testingPhaseEnded ? "Live" : "Testphase")}";
    }
}
