// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class ComparisonVotingChannelConfiguration : BaseEntity
{
    public VotingChannel VotingChannel { get; set; }

    public decimal? ThresholdPercent { get; set; }

    public PlausibilisationConfiguration PlausibilisationConfiguration { get; set; } = null!;

    public Guid PlausibilisationConfigurationId { get; set; }
}
