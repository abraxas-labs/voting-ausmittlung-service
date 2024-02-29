// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public static class ContestStateExtensions
{
    public static bool IsLocked(this ContestState contestState)
        => contestState is ContestState.PastLocked or ContestState.Archived;

    public static bool IsActiveOrUnlocked(this ContestState contestState)
        => contestState is ContestState.Active or ContestState.PastUnlocked;

    public static bool TestingPhaseEnded(this ContestState state) => state > ContestState.TestingPhase;
}
