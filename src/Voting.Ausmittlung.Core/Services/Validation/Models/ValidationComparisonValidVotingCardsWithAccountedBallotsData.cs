// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Services.Validation.Models;

public class ValidationComparisonValidVotingCardsWithAccountedBallotsData
{
    public decimal DeviationPercent { get; set; }

    public decimal ThresholdPercent { get; set; }

    public PoliticalBusinessType PoliticalBusinessType { get; set; }
}
