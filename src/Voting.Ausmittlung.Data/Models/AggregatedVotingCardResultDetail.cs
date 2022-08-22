// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public abstract class AggregatedVotingCardResultDetail : BaseEntity, IVotingCardChannel
{
    public int CountOfReceivedVotingCards { get; set; }

    public bool Valid { get; set; }

    public VotingChannel Channel { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }
}
