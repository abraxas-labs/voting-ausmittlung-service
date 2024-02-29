// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// The voting card details for an end result.
/// Voting card details for an end result may not match the voting card details of the contest (see <see cref="VotingCardResultDetail"/>),
/// because voting cards only count towards an end result if the linked counting circle is in a certain state.
/// </summary>
public abstract class EndResultVotingCardDetail : BaseEntity, IVotingCardChannel
{
    public int? CountOfReceivedVotingCards { get; set; }

    public bool Valid { get; set; }

    public VotingChannel Channel { get; set; }

    public DomainOfInfluenceType DomainOfInfluenceType { get; set; }
}
