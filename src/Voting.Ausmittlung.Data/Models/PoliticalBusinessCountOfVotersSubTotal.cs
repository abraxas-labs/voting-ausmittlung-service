// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessCountOfVotersSubTotal : ISummableSubTotal<PoliticalBusinessCountOfVotersSubTotal>
{
    public int ReceivedBallots { get; set; }

    public int InvalidBallots { get; set; }

    public int BlankBallots { get; set; }

    public int AccountedBallots { get; set; }

    public void MoveAccountedBallotsToInvalid(int count)
    {
        InvalidBallots += count;
        AccountedBallots -= count;
    }

    public void Add(PoliticalBusinessCountOfVotersSubTotal other, int deltaFactor = 1)
    {
        ReceivedBallots += other.ReceivedBallots * deltaFactor;
        InvalidBallots += other.InvalidBallots * deltaFactor;
        BlankBallots += other.BlankBallots * deltaFactor;
        AccountedBallots += other.AccountedBallots * deltaFactor;
    }
}
