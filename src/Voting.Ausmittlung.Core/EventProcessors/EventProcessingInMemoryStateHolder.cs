// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Voting.Ausmittlung.Data.Models;

namespace Voting.Ausmittlung.Core.EventProcessors;

public class EventProcessingInMemoryStateHolder
{
    internal EventProcessingState? State { get; private set; }

    internal void SetState(EventProcessingState state)
    {
        if (State != null && state.EventNumber < State.EventNumber)
        {
            throw new InvalidOperationException(
                $"Cannot set in-memory state with lower event number than the current state (In-Memory: {State.EventNumber}, New: {state.EventNumber}).");
        }

        State = state;
    }

    internal void ResetState()
    {
        State = null;
    }
}
