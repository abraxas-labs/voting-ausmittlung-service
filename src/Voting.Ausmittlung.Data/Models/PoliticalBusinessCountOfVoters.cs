// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessCountOfVoters : IHasSubTotals<PoliticalBusinessCountOfVotersSubTotal>
{
    public decimal VoterParticipation { get; set; }

    public PoliticalBusinessCountOfVotersSubTotal EVotingSubTotal { get; set; } = new();

    public PoliticalBusinessCountOfVotersSubTotal ECountingSubTotal { get; set; } = new();

    public PoliticalBusinessCountOfVotersSubTotal ConventionalSubTotal { get; set; } = new();

    public int TotalReceivedBallots
    {
        get => ConventionalSubTotal.ReceivedBallots
               + EVotingSubTotal.ReceivedBallots
               + ECountingSubTotal.ReceivedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalAccountedBallots
    {
        get => ConventionalSubTotal.AccountedBallots
               + EVotingSubTotal.AccountedBallots
               + ECountingSubTotal.AccountedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalUnaccountedBallots
    {
        get => ConventionalSubTotal.BlankBallots
               + ConventionalSubTotal.InvalidBallots
               + EVotingSubTotal.BlankBallots
               + EVotingSubTotal.InvalidBallots
               + ECountingSubTotal.BlankBallots
               + ECountingSubTotal.InvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalInvalidBallots
    {
        get => ConventionalSubTotal.InvalidBallots
               + EVotingSubTotal.InvalidBallots
               + ECountingSubTotal.InvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalBlankBallots
    {
        get => ConventionalSubTotal.BlankBallots + EVotingSubTotal.BlankBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public static PoliticalBusinessCountOfVoters CreateSum(IEnumerable<PoliticalBusinessCountOfVoters> items)
    {
        var sum = new PoliticalBusinessCountOfVoters();
        foreach (var item in items)
        {
            foreach (var (dataSource, subTotal) in sum.SubTotalAsEnumerable())
            {
                var otherSubTotal = item.GetSubTotal(dataSource);
                subTotal.AccountedBallots += otherSubTotal.AccountedBallots;
                subTotal.ReceivedBallots += otherSubTotal.ReceivedBallots;
                subTotal.InvalidBallots += otherSubTotal.InvalidBallots;
                subTotal.BlankBallots += otherSubTotal.BlankBallots;
            }
        }

        return sum;
    }

    public void UpdateVoterParticipation(int totalCountOfVoters)
    {
        // total count of voters cannot be negative, checked by business rules
        if (totalCountOfVoters == 0)
        {
            VoterParticipation = 0;
            return;
        }

        // round to 6 decimal places has to be in sync with frontend
        VoterParticipation = Math.Round(TotalReceivedBallots / (decimal)totalCountOfVoters, 6);
    }

    public void ResetSubTotal(VotingDataSource dataSource, int totalCountOfVoters)
    {
        this.ResetSubTotal(dataSource);
        UpdateVoterParticipation(totalCountOfVoters);
    }
}
