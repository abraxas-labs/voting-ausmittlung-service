// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Data.Models;

public class ContestDomainOfInfluenceDetails : AggregatedContestCountingCircleDetails<DomainOfInfluenceCountOfVotersInformationSubTotal, DomainOfInfluenceVotingCardResultDetail>
{
    public Guid DomainOfInfluenceId { get; set; }

    public DomainOfInfluence DomainOfInfluence { get; set; } = null!;

    public Guid ContestId { get; set; }

    public Contest Contest { get; set; } = null!;
}
