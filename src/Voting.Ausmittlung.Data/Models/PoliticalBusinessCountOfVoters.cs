// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessCountOfVoters
{
    public decimal VoterParticipation { get; set; }

    public int EVotingReceivedBallots { get; set; }

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
        get => ConventionalAccountedBallots + EVotingReceivedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalUnaccountedBallots
    {
        get => ConventionalBlankBallots + ConventionalInvalidBallots;
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

        VoterParticipation = TotalReceivedBallots / (decimal)totalCountOfVoters;
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
                break;
        }

        UpdateVoterParticipation(totalCountOfVoters);
    }
}
