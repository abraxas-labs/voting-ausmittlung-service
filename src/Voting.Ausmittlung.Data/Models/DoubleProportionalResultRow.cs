// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Collections.Generic;
using Voting.Lib.Database.Models;

namespace Voting.Ausmittlung.Data.Models;

/// <summary>
/// A row of a double proportional result matrix. Represents a proportional election.
/// </summary>
public class DoubleProportionalResultRow : BaseEntity
{
    public Guid ProportionalElectionId { get; set; }

    public ProportionalElection ProportionalElection { get; set; } = null!;

    /// <summary>
    /// Gets or sets the vote count from all lists (including the vote count from lists which did not pass the "Quorum" step).
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating the "Wählerzahl" (Sum of all election lists).
    /// </summary>
    public decimal VoterNumber { get; set; }

    /// <summary>
    /// Gets or sets the quorum of the proportional election. If a <see cref="ProportionalElectionList"/> has at least that many votes, the corresponding
    /// <see cref="ProportionalElectionUnionList"/> can participate in the number of mandates distribution.
    /// </summary>
    public int Quorum { get; set; }

    /// <summary>
    /// Gets or sets the "Domain of Influence" divider from the "Unterzuteilung".
    /// </summary>
    public decimal Divisor { get; set; }

    public int NumberOfMandates { get; set; }

    /// <summary>
    /// Gets or sets the distributed number of mandates. Could differ from <see cref="ProportionalElection.NumberOfMandates"/> if the "Unterzuteilung" was not able to distribute all.
    /// </summary>
    public int SubApportionmentNumberOfMandates { get; set; }

    public ICollection<DoubleProportionalResultCell> Cells { get; set; } = new HashSet<DoubleProportionalResultCell>();

    public Guid ResultId { get; set; }

    public DoubleProportionalResult Result { get; set; } = null!;
}
