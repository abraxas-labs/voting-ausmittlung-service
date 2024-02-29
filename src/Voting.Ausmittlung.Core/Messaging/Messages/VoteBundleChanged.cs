// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

// the name of the rabbit queue is generated based on the fqn of this class
// if it is moved or renamed this should be considered.
public class VoteBundleChanged
{
    public VoteBundleChanged(Guid id, Guid ballotResultId)
    {
        Id = id;
        BallotResultId = ballotResultId;
    }

    public Guid Id { get; }

    public Guid BallotResultId { get; }
}
