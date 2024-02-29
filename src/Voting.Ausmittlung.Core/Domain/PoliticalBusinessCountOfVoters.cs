// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

namespace Voting.Ausmittlung.Core.Domain;

public class PoliticalBusinessCountOfVoters
{
    /// <summary>
    /// Gets or sets the number of received ballots via a conventional channel.
    /// </summary>
    public int? ConventionalReceivedBallots { get; set; }

    /// <summary>
    /// Gets or sets the number of invalid ballots received via a conventional channel.
    /// </summary>
    public int? ConventionalInvalidBallots { get; set; }

    /// <summary>
    /// Gets or sets the number of blank ballots received via a conventional channel.
    /// </summary>
    public int? ConventionalBlankBallots { get; set; }

    /// <summary>
    /// Gets or sets the number of accounted ballots received via a conventional channel.
    /// </summary>
    public int? ConventionalAccountedBallots { get; set; }
}
