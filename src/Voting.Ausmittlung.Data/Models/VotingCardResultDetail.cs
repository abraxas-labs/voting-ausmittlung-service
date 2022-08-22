// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class VotingCardResultDetail : BaseEntity
{
    public Guid ContestCountingCircleDetailsId { get; set; }

    public ContestCountingCircleDetails ContestCountingCircleDetails { get; set; } = null!;

    public int? CountOfReceivedVotingCards { get; set; }

    public bool Valid { get; set; }

    public VotingChannel Channel { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }
}
