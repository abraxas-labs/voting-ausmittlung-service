// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Domain;

public class VotingCardResultDetail
{
    public int? CountOfReceivedVotingCards { get; set; }

    public bool Valid { get; set; }

    public VotingChannel Channel { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }
}
