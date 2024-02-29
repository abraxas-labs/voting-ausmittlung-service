// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public interface IVotingCardChannel
{
    public bool Valid { get; }

    public VotingChannel Channel { get; }
}
