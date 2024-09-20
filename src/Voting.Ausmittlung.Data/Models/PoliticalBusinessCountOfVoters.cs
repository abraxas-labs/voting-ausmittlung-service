// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessCountOfVoters
{
    public decimal VoterParticipation { get; set; }

    public int EVotingReceivedBallots { get; set; }

    public int EVotingInvalidBallots { get; set; }

    public int EVotingBlankBallots { get; set; }

    public int EVotingAccountedBallots { get; set; }

    public int ConventionalReceivedBallots { get; set; }

    public int ConventionalInvalidBallots { get; set; }

    public int ConventionalBlankBallots { get; set; }

    public int ConventionalAccountedBallots { get; set; }

    public int TotalReceivedBallots
    {
        get => ConventionalReceivedBallots + EVotingReceivedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalAccountedBallots
    {
        get => ConventionalAccountedBallots + EVotingAccountedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalUnaccountedBallots
    {
        get => ConventionalBlankBallots + ConventionalInvalidBallots + EVotingBlankBallots + EVotingInvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalInvalidBallots
    {
        get => ConventionalInvalidBallots + EVotingInvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalBlankBallots
    {
        get => ConventionalBlankBallots + EVotingBlankBallots;
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
        switch (dataSource)
        {
            case VotingDataSource.Conventional:
                ConventionalAccountedBallots = 0;
                ConventionalBlankBallots = 0;
                ConventionalInvalidBallots = 0;
                ConventionalReceivedBallots = 0;
                break;
            case VotingDataSource.EVoting:
                EVotingReceivedBallots = 0;
                EVotingInvalidBallots = 0;
                EVotingBlankBallots = 0;
                EVotingAccountedBallots = 0;
                break;
        }

        UpdateVoterParticipation(totalCountOfVoters);
    }
}
