// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public static class PlausibilisationConfigurationExtensions
{
    /// <summary>
    /// Snapshots a plausibilisation configuration. Modifies the party in place.
    /// </summary>
    /// <param name="plausiConfig">The configuration to snapshot.</param>
    public static void SnapshotForContest(this PlausibilisationConfiguration plausiConfig)
    {
        // Modify the IDs. When saving this configuration to the database, this will create new entries.
        var id = Guid.NewGuid();
        plausiConfig.Id = id;

        foreach (var comparisonVoterParticipationConfigurations in plausiConfig.ComparisonVoterParticipationConfigurations)
        {
            comparisonVoterParticipationConfigurations.Id = Guid.NewGuid();
            comparisonVoterParticipationConfigurations.PlausibilisationConfigurationId = id;
        }

        foreach (var comparisonCountOfVotersConfiguration in plausiConfig.ComparisonCountOfVotersConfigurations)
        {
            comparisonCountOfVotersConfiguration.Id = Guid.NewGuid();
            comparisonCountOfVotersConfiguration.PlausibilisationConfigurationId = id;
        }

        foreach (var comparisonVotingChannelConfiguration in plausiConfig.ComparisonVotingChannelConfigurations)
        {
            comparisonVotingChannelConfiguration.Id = Guid.NewGuid();
            comparisonVotingChannelConfiguration.PlausibilisationConfigurationId = id;
        }
    }
}
