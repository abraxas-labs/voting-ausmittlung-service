// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessCountOfVotersNullableSubTotal :
    INullableSubTotal<PoliticalBusinessCountOfVotersSubTotal>,
    ISummableSubTotal<PoliticalBusinessCountOfVotersSubTotal>
{
    public int? ReceivedBallots { get; set; }

    public int? InvalidBallots { get; set; }

    public int? BlankBallots { get; set; }

    public int? AccountedBallots { get; set; }

    public PoliticalBusinessCountOfVotersSubTotal MapToNonNullableSubTotal()
    {
        return new PoliticalBusinessCountOfVotersSubTotal
        {
            AccountedBallots = AccountedBallots ?? 0,
            ReceivedBallots = ReceivedBallots ?? 0,
            InvalidBallots = InvalidBallots ?? 0,
            BlankBallots = BlankBallots ?? 0,
        };
    }

    public void ReplaceNullValuesWithZero()
    {
        ReceivedBallots ??= 0;
        InvalidBallots ??= 0;
        BlankBallots ??= 0;
        AccountedBallots ??= 0;
    }

    public void Add(PoliticalBusinessCountOfVotersSubTotal other, int deltaFactor = 1)
    {
        ReplaceNullValuesWithZero();
        InvalidBallots += other.InvalidBallots * deltaFactor;
        ReceivedBallots += other.ReceivedBallots * deltaFactor;
        BlankBallots += other.BlankBallots * deltaFactor;
        AccountedBallots += other.AccountedBallots * deltaFactor;
    }
}
