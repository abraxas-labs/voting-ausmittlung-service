// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

public class DomainOfInfluenceCantonDefaultsVotingCardChannel : BaseEntity, IVotingCardChannel
{
    public VotingChannel VotingChannel { get; set; }

    public bool Valid { get; set; }

    public VotingChannel Channel => VotingChannel;
}
