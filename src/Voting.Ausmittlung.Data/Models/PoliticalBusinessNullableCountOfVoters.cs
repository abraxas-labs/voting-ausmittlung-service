// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public class PoliticalBusinessNullableCountOfVoters : INullableSubTotal<PoliticalBusinessCountOfVoters>
{
    public decimal VoterParticipation { get; set; }

    public int EVotingReceivedBallots { get; set; }

    public int EVotingInvalidBallots { get; set; }

    public int EVotingBlankBallots { get; set; }

    public int EVotingAccountedBallots { get; set; }

    public int? ConventionalReceivedBallots { get; set; }

    public int? ConventionalInvalidBallots { get; set; }

    public int? ConventionalBlankBallots { get; set; }

    public int? ConventionalAccountedBallots { get; set; }

    public int TotalReceivedBallots
    {
        get => ConventionalReceivedBallots.GetValueOrDefault() + EVotingReceivedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalAccountedBallots
    {
        get => ConventionalAccountedBallots.GetValueOrDefault() + EVotingAccountedBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalUnaccountedBallots
    {
        get => ConventionalBlankBallots.GetValueOrDefault()
            + ConventionalInvalidBallots.GetValueOrDefault()
            + EVotingBlankBallots
            + EVotingInvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalInvalidBallots
    {
        get => ConventionalInvalidBallots.GetValueOrDefault() + EVotingInvalidBallots;
        private set
        {
            // empty setter to store the value in the database...
        }
    }

    public int TotalBlankBallots
    {
        get => ConventionalBlankBallots.GetValueOrDefault() + EVotingBlankBallots;
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
                EVotingInvalidBallots = 0;
                EVotingBlankBallots = 0;
                EVotingAccountedBallots = 0;
                break;
        }

        UpdateVoterParticipation(totalCountOfVoters);
    }

    public PoliticalBusinessCountOfVoters MapToNonNullableSubTotal()
    {
        return new PoliticalBusinessCountOfVoters
        {
            VoterParticipation = VoterParticipation,
            ConventionalAccountedBallots = ConventionalAccountedBallots.GetValueOrDefault(),
            ConventionalBlankBallots = ConventionalBlankBallots.GetValueOrDefault(),
            ConventionalInvalidBallots = ConventionalInvalidBallots.GetValueOrDefault(),
            ConventionalReceivedBallots = ConventionalReceivedBallots.GetValueOrDefault(),
            EVotingReceivedBallots = EVotingReceivedBallots,
            EVotingInvalidBallots = EVotingInvalidBallots,
            EVotingBlankBallots = EVotingBlankBallots,
            EVotingAccountedBallots = EVotingAccountedBallots,
        };
    }

    public void ReplaceNullValuesWithZero()
    {
        ConventionalAccountedBallots ??= 0;
        ConventionalBlankBallots ??= 0;
        ConventionalInvalidBallots ??= 0;
        ConventionalReceivedBallots ??= 0;
    }
}
