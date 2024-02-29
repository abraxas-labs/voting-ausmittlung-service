// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.Messaging.Messages;

// the name of the rabbit queue is generated based on the fqn of this class
// if it is moved or renamed this should be considered.
public class ResultStateChanged
{
    public ResultStateChanged(Guid id, Guid countingCircleId, Guid politicalBusinessId, CountingCircleResultState newState)
    {
        Id = id;
        CountingCircleId = countingCircleId;
        PoliticalBusinessId = politicalBusinessId;
        NewState = newState;
    }

    public Guid Id { get; }

    public Guid CountingCircleId { get; }

    public Guid PoliticalBusinessId { get; }

    public CountingCircleResultState NewState { get; }
}
