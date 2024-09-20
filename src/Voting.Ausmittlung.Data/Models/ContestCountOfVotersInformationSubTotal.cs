// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ContestCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal
{
    public Guid ContestDetailsId { get; set; }

    public ContestDetails ContestDetails { get; set; } = null!;
}
