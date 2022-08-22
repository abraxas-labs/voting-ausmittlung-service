// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum ContestState
{
    /// <summary>
    /// contest state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// contest is in testing phase.
    /// </summary>
    TestingPhase,

    /// <summary>
    /// contest takes place in the future or today, but is not in the testing phase anymore.
    /// </summary>
    Active,

    /// <summary>
    /// contest has taken place in the past and is locked.
    /// </summary>
    PastLocked,

    /// <summary>
    /// contest has taken place in the past and is unlocked, but it will automatically get locked after the day ends.
    /// </summary>
    PastUnlocked,

    /// <summary>
    /// contest is archived.
    /// </summary>
    Archived,
}
