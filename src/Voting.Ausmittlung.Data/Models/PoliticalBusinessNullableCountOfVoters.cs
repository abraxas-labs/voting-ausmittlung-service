// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessNullableCountOfVoters :
    INullableSubTotal<PoliticalBusinessCountOfVoters>,
    IHasSubTotals<PoliticalBusinessCountOfVotersSubTotal, PoliticalBusinessCountOfVotersNullableSubTotal>
{
    public decimal VoterParticipation { get; set; }

    public PoliticalBusinessCountOfVotersSubTotal EVotingSubTotal { get; set; } = new();

    public PoliticalBusinessCountOfVotersSubTotal ECountingSubTotal { get; set; } = new();

    public PoliticalBusinessCountOfVotersNullableSubTotal ConventionalSubTotal { get; set; } = new();

    public int TotalReceivedBallots
    {
        get => ConventionalSubTotal.ReceivedBallots.GetValueOrDefault() + EVotingSubTotal.ReceivedBallots + ECountingSubTotal.ReceivedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalAccountedBallots
    {
        get => ConventionalSubTotal.AccountedBallots.GetValueOrDefault() + EVotingSubTotal.AccountedBallots + ECountingSubTotal.AccountedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalUnaccountedBallots
    {
        get => ConventionalSubTotal.BlankBallots.GetValueOrDefault()
            + ConventionalSubTotal.InvalidBallots.GetValueOrDefault()
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
        get => ConventionalSubTotal.InvalidBallots.GetValueOrDefault() + EVotingSubTotal.InvalidBallots + ECountingSubTotal.InvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalBlankBallots
    {
        get => ConventionalSubTotal.BlankBallots.GetValueOrDefault() + EVotingSubTotal.BlankBallots + ECountingSubTotal.BlankBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
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

    public PoliticalBusinessCountOfVoters MapToNonNullableSubTotal()
    {
        return new PoliticalBusinessCountOfVoters
        {
            VoterParticipation = VoterParticipation,
            ConventionalSubTotal = ConventionalSubTotal.MapToNonNullableSubTotal(),
            ECountingSubTotal = ECountingSubTotal,
            EVotingSubTotal = EVotingSubTotal,
        };
    }

    public void ReplaceNullValuesWithZero() => ConventionalSubTotal.ReplaceNullValuesWithZero();
}
