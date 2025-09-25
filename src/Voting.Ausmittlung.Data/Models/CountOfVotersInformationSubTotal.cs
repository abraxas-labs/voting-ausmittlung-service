// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class CountOfVotersInformationSubTotal : BaseEntity
{
    public SexType Sex { get; set; } = SexType.Undefined;

    public int? CountOfVoters { get; set; }

    public VoterType VoterType { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }

    public Guid ContestCountingCircleDetailsId { get; set; }

    public ContestCountingCircleDetails ContestCountingCircleDetails { get; set; } = null!;
}
