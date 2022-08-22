// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceCountOfVotersInformationSubTotal : AggregatedCountOfVotersInformationSubTotal
{
    public Guid ContestDomainOfInfluenceDetailsId { get; set; }

    public ContestDomainOfInfluenceDetails ContestDomainOfInfluenceDetails { get; set; } = null!;
}
