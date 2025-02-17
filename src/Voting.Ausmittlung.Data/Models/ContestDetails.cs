// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ContestDetails : AggregatedContestCountingCircleDetails<ContestCountOfVotersInformationSubTotal, ContestVotingCardResultDetail>
{
    public Guid ContestId { get; set; }

    public Contest Contest { get; set; } = null!;
}
