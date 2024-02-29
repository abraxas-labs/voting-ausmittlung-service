// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Data.Models;

public enum MajorityElectionReviewProcedure
{
    /// <summary>
    /// Majority election review procedure is unspecified.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The review procedure is performed electronically.
    /// </summary>
    Electronically,

    /// <summary>
    /// The review procedure is performed physically.
    /// </summary>
    Physically,
}
