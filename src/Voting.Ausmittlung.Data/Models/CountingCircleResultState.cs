// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum CountingCircleResultState
{
    /// <summary>
    /// Counting circle result state is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Counting circle result is in initial state.
    /// </summary>
    Initial,

    /// <summary>
    /// Counting circle result submission is ongoing.
    /// </summary>
    SubmissionOngoing,

    /// <summary>
    /// Counting circle result is ready for correction.
    /// </summary>
    ReadyForCorrection,

    /// <summary>
    /// Counting circle result submission is done.
    /// </summary>
    SubmissionDone,

    /// <summary>
    /// Counting circle result correction is done.
    /// </summary>
    CorrectionDone,

    /// <summary>
    /// Counting circle result was audited tentatively.
    /// </summary>
    AuditedTentatively,

    /// <summary>
    /// Counting circle result is plausibilised.
    /// </summary>
    Plausibilised,
}
