// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationComparisonVotingChannelsData
{
    public decimal ThresholdPercent { get; set; }

    public decimal DeviationPercent { get; set; }

    public int Deviation { get; set; }

    public DateTime PreviousDate { get; set; }

    public int PreviousCount { get; set; }

    public int CurrentCount { get; set; }

    public VotingChannel VotingChannel { get; set; }
}
