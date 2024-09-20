// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ContestVotingCardResultDetail : AggregatedVotingCardResultDetail
{
    public Guid ContestDetailsId { get; set; }

    public ContestDetails ContestDetails { get; set; } = null!;
}
